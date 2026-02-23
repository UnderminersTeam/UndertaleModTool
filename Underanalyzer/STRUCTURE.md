# How Underanalyzer's GML decompiler works

## Introduction

### A simple (but large) iterative decompiler

Underanalyzer is a decompiler that iterates over any given code entry many times. In each pass, either more information is discovered (directly or by process of elimination), or internal structures are updated to reflect new information.

The main iterations the decompiler performs, on any given code entry, are:
1. Determine all control flow constructs.
2. Build a complete high-level GML AST, given control flow information.
3. Clean up the GML AST, removing compiler-generated constructs and simplifying code.

By far, the most complex iteration is determining control flow constructs. Itself, it is divided into iterations:
1. Determine all basic blocks (no control flow), and their graph connections. This creates a control flow graph, composed only of these basic blocks at this point, but which will be heavily modified in the following steps.
2. Group basic blocks into "fragments," representing function scopes (before GMLv2, there is only ever one fragment in a code entry, encompassing all blocks).
3. Find all static initialization blocks (`static` keyword in GMLv2), and insert new nodes for them into the control flow graph.
4. Find all nullish-coalescing operator blocks (`??` and `??=` operators in GMLv2), and insert new nodes for them into the control flow graph.
5. Find the ending (final) basic blocks of all short-circuit operations (`&&` and `||` operators, when short-circuiting is enabled).
6. Find all loops (`while`/`for`, `do...until`, `repeat`, `with`), and reconstruct control flow graph to account for them.
7. Use the previously-discovered ending blocks of short-circuit operations (`&&` and `||`) to reconstruct the control flow graph, accounting for them.
8. Find all `try`/`catch`/`finally` statements, and reconstruct control flow graph to account for them.
9. Find all `switch` statements, and determine important details about them for later processing.
10. Resolve many common `break`/`continue` statements, by computing limits on how far any given unconditional branch can go without being `break`/`continue`.
11. Find all binary branches (`if`, conditional expressions), and reconstruct control flow graph to account for them.
12. Using earlier `switch` statement information, reconstruct control flow graph to account for them.
13. Resolve all remaining unidentified `continue` statements, those being all remaining unconditional branches in the control flow graph.
14. Clean up some control flow leading to the end of a `try` statement, which should no longer exist.

By the end of all of those steps, Underanalyzer has enough information to build an AST.
The control flow graph is essentially a tree itself, just formatted with predecessors and successors.
The AST building phase recursively transforms every type of control flow to corresponding tree nodes.

Finally, a clean-up process is recursively applied to the AST, which can then be returned or printed as GML code.

### Initial development process

Before getting into further details, it's useful to know the process by which Underanalyzer was developed. Most of the algorithms were created through a "this is how I would reverse engineer this if I were reading the assembly myself" approach. There's ideas like "this branch instruction couldn't possibly jump that far without being a `continue` statement." Obviously, that's a pretty naive approach, but Underanalyzer has plenty of tests to back up its correctness.

In case it isn't obvious from the above section, the control flow section was the most difficult to get right. One large assumption the decompiler makes, as opposed to other existing GML decompilers, is the order of blocks in bytecode being predictable; that the blocks follow the order of statements in the source code. This assumption has two benefits: it simplifies algorithms, and it allows for highly accurate output that can be re-compiled identically. The drawback is that basic obfuscated code (or non-standard bytecode) is out of scope, but given you could still obfuscate code in any number of other ways, it is not a priority here.

The other closely-related assumption is that specific instruction patterns are consistent, with very similar benefits and drawbacks. The decompiler does not account for all kinds of bytecode that can technically run - it only focuses on real-world, official GML compiler output.

## Control flow steps

### Finding basic blocks

This initial step lays out the foundation on which the rest of decompilation occurs. A basic block, or just a "block" here, is a contiguous set of instructions with no branches (either to or from any instruction), except for the first and last instructions.

The algorithm to do this is straightforward:
1. Collect a unique set of all block start addresses. (Build a hash set from all branch target addresses.)
2. Create block nodes for all start addresses in the set, and associate them with their instructions. Connect each block node to their predecessors and successors, building a control flow graph.
3. For all block nodes with no predecessors, mark them as unreachable, and connect to the block immediately preceding them (in terms of address). *Note that this relies on a consistent block order.*

Once this is complete, you have a full control flow graph, without any distinguished high-level concepts.

### Find fragments

This step groups blocks into "fragments." A fragment, in this context, is a section of a GML code entry that is designated to the scope of one function/execution context. That is, each fragment corresponds 1-to-1 with a code entry (or compiler-generated code entry) in a game's data. For games prior to GMLv2, functions don't quite exist, so there's always only one fragment for each code entry (or to clarify, generally per each GML file).

Here, the algorithm is slightly more complicated:
1. Identify all fragment addresses (easy - just follow the game's code entry data).
2. Loop over all basic blocks, and associate all blocks to their corresponding fragments.
    * Because these blocks are not all contiguous, a stack can be used to track depth (e.g., to return to the enclosing fragment when done with the current one).
3. Mark all blocks at the start of a fragment as reachable (if they were previously unreachable).
4. Insert all fragments into control flow graph, rerouting predecessors/successors of existing blocks.

Once this is complete, blocks are properly categorized per each function, and each fragment is connected to the overall control flow graph.

### Find and insert static initialization nodes

This step handles recognition and processing of the `static` keyword introduced in GMLv2. Due to their predictable instruction pattern, they are relatively easy to deal with:
1. Detect `Opcode.Extended` with `ExtendedOpcode.HasStaticInitialized`, followed by `Opcode.BranchTrue`, at the end of any given block.
2. For all detections, process and insert new nodes into the control flow graph (rerouting existing predecessors/successors).
    * Remove the two matched `Opcode.Extended` and `Opcode.BranchTrue` instructions (they are no longer useful).
    * Remove an additional `ExtendedOpcode.ResetStatic` instruction, if present (it, too, is not useful). *This being present depends on GML compiler version.*
    * Remove connections related to internal control flow (e.g., the `Opcode.BranchTrue` instruction in this case).

Once this is complete, static initialization nodes are now a part of the control flow graph, and all branches related to them are eliminated.

### Find and insert nullish-coalescing operator nodes

This step is very similar, but slightly more involved, than the previous step. It handles control flow and branches related to the `??` and `??=` operators:
1. Detect `Opcode.Extended` with `ExtendedOpcode.IsNullishValue`, followed by `Opcode.BranchFalse`, at the end of any given block.
2. For all detections, process and insert new nodes into the control flow graph (rerouting existing predecessors/successors).
    * Remove the two matched `Opcode.Extended` and `Opcode.BranchFalse` instructions (they are no longer useful).
    * Remove connections and instructions related to internal control flow.
    * Route all internal nodes which exit the internal control flow to a new "empty node." This prevents later algorithms from erroneously affecting internal control flow.
    * Some further details are omitted here, but the difference between `??` and `??=` can be detected, and relevant unnecessary `Opcode.PopDelete` instructions removed.

Once this is complete, nullish-coalescing operator nodes are now a part of the control flow graph, and all branches related to them are eliminated.

### Find the ends of all short-circuit (`&&` and `||`) operations

This step is very straightforward. The final block of any short-circuit operation can consistently be located using a block that contains **only** a `Opcode.Push` (or on older versions, `Opcode.PushImmediate`) instruction, with `DataType.Int16`. By sheer coincidence, it seems that this rudimentary detection method does not trigger any false-positives.

Note that this step *only* collects a list of these blocks, for later processing.

### Find and insert loop nodes

This step involves finding *every* type of loop in a code entry. Amazingly, detection methods are similar to previous steps, and rather straightforward:
- `while` (and `for`) loops: Check for a block ending with `Opcode.Branch`, which jumps backwards.
- `do...until` loops: Check for a block ending with `Opcode.BranchFalse`, which jumps backwards.
- `repeat` loops: Check for a block ending with `Opcode.BranchTrue`, which jumps backwards.
- `with` loops: Check for a block ending with `Opcode.PushWithContext`.

Some details are omitted here, but each loop type will track information specific to its own processing.

After all loops are detected, they are sorted in order from inner to outer, and first to last. This ensures loops are processed in a consistent order.

Finally, all loop nodes are inserted into the control flow graph, in the sorted order. Each loop type does this differently, but by the end, all nodes internal to a loop are disconnected from nodes external to their loop, and all branches relating to the loop (aside from `break`/`continue`) are eliminated.

### Insert short circuit nodes (`&&` and `||`)

This step uses the existing short-circuit operations that were discovered in an earlier step, and creates nodes for each one. The nodes are inserted into the control flow graph, and all connections crossing in/out of the short circuit's internal blocks are disconnected. Instructions that are no longer necesssary are also removed. There's a bunch of steps that go into this, which are omitted here.

### Find and insert `try`/`catch`/`finally` nodes

This step involves detecting `try` statements, using simple pattern matching for a block containing a `@@try_hook@@` call. Upon discovery, the control flow graph is updated with a new node for the statement, and instructions that are no longer required are removed. Similarly to all other control flow nodes, all connections crossing in/out of the statement's internal blocks are disconnected. Many details are again omitted here, for simplicity.

There is one special aspect of `try` statements, which is that while `finally` blocks *are* detected in this phase (and they have one instruction removed), they are *not* yet associated with the `try` statement they are part of. The reason for this is that, for all intents and purposes, the `finally` block is emitted *after* the main blocks of the `try` statement. When you factor in other (possibly unresolved!) statements with control flow, and especially `try` statements nested inside of a `finally` block, it is infeasible to resolve the full `finally` block at this stage. Instead, it is done during AST generation, where block scopes are easy to work with.

### Find all `switch` statements

This step finds all `switch` statements in a code entry, along with tracking some related information that is used later.

First, a detection pass is performed. This pass detects the ending blocks of all `switch` statements, as well as the blocks generated when using `continue` inside of a `switch` statement (which are generated to perform stack cleanup). This is done on its own in order to eliminate any possible false positives due to edge cases. Details are omitted here, but it's essentially a mix of pattern matching and process of elimination.

Second, a detail pass is performed. This pass identifies a few key blocks for every `switch` statement, namely the "end of case" and "default branch" blocks. The "end of case" block is a block always emitted for every statement, after the chain of cases is emitted. The "default branch" block is a block emitted only when `default` is used as a case in a statement. This is done after the detection pass, as part of this requires knowledge of all switch ending blocks.

As mentioned above, some of these blocks related to switch statements are useful in the following steps, to refine detections (especially with `continue`/`break` statements).

#### Aside: "Find surrounding loops" algorithm

Part of the detail pass involves knowledge of loops that immediately surround a `switch` statement. For this purpose, a simple loop is employed that maps each basic block to the loop node immediately surrounding it (if at least one exists). Inner/nested loops are given priority, so they appear later in the list of loops that the decompiler keeps track of.

### Find and insert binary branch (`if`/conditionals) nodes

Up until this point, almost all types of control flow have been transformed into high-level nodes in the control flow graph. Specifically, this excludes binary branches (this step), `break`/`continue` statements (most of which will be covered by this step), and `switch` statements. This, combined with the reliance on a specific block order, allows for some simple algorithms to work with binary branches.

#### But first, resolving "external jumps"

Before resolving binary branches, it's very convenient to eliminate as many possible `break`/`continue` statements, and technically also `return`/`exit`. We want to rewrite them with custom nodes, but to flow as if they don't actually jump anywhere. If this is done, the control flow graph is massively simplified: the two branches of any binary branch are then guaranteed to meet at some location. (Rather than, for instance, a `break` statement in one of the branches throwing everything off.)

For `return`/`exit`, GML bytecode graces us with `Opcode.Return` and `Opcode.Exit`. They're trivial to detect, so it just requires updating the control flow graph to insert their nodes.

For `break`/`continue`, it's a bit tricky. We know that they use `Opcode.Branch`, though. With that as a starting point, we can run down some logic:
- If the branch is from a basic block marked as a special part of a `switch` statement, we ignore it.
- If the branch jumps to a loop immediately surrounding itself, it's trivially a `continue` statement. (See the "Find surrounding loops" algorithm.)
- If the branch jumps past the end of the loop immediately surrounding itself, it's trivially a `break` statement.
- If the branch jumps to the ending block of a `switch` statement, it's trivially a `break`.
- If the branch jumps to the "continue block" of a `switch` statement, it's trivially a `continue`.
- If the branch jumps past its maximum "after limit," it's a `continue`. Wait, what's an "after limit?"

(Small note before getting into what an "after limit" is: `continue` statements, depending on context, can transform a `for` loop into a `while` and vice versa, depending on context; code generation slightly differs.)

#### But *first*, computing "after limits" for all blocks

This is perhaps one of the weirdest algorithms of the whole decompiler, and one that exemplifies the philosophy of "this is how I would do this if I were doing it by hand." Essentially, this is an algorithm that determines the maximum address a given branch can jump to, before it is guaranteed to be a `continue` statement.

This works by maintaining a stack of "limit entries," while looping over all basic blocks. Upon reaching the address of a "limit entry" in the loop, it is removed from the stack. Each "limit entry" is just composed of the address of the limit, plus whether or not it was generated from a binary branch (`Opcode.BranchTrue`/`Opcode.BranchFalse`) or not. Without getting into the specifics and edge cases, the limits are constrained by these two types of branches, as well as surrounding loops (again, from the "Find surrounding loops" algorithm).

The end result is that we can determine many more cases of `continue` statements - at the very least, we get all the ones we require before processing binary branches. There's a few that remain, to be later detected.

#### Back to binary branches!

Now that "external jumps" are mostly taken care of, we can begin dealing with binary branches. In this case, we only care about `Opcode.BranchFalse`, which is used by `if` statements and conditionals (`a ? b : c`).

Similarly to dealing with all other control flow types, we detect and insert nodes into the control flow graph. With binary branches, we use a simple algorithm, going in *reverse* order of basic blocks (taking advantage of the control flow graph being in order and acyclic):
- Make a set for all control flow nodes that have been visited by this algorithm (starting empty).
- For all basic blocks ending in `Opcode.BranchFalse`:
    * Visit all nodes that are reachable from the "false"/"jump" branch.
    * Visit all nodes that are reachable from the "true"/"non-jump" branch, stopping upon finding an already-visited node which is at least beyond the address of the "false"/"jump" branch destination. This node is the "meetpoint" of the binary branch.
    * Final touches! If the falsey branch is not the same node as the meetpoint, then there's an `else` in there (or it's a conditional). If the truthy branch is the same node as the meetpoint, then we have an empty if statement.

The rest is roughly similar to rerouting control flow for other types of nodes, but I won't get into the details here.

### Insert all `switch` statement nodes

Using the basic detection data from earlier on, this step can now insert the nodes for `switch` statements into the control flow graph. This one isn't very different than the others, however due to the sheer size of the structure, there is a lot to do.

First, all `case` branches are collected. This may seem trivial if you know how the equivalent GML bytecode looks, but GML has a few surprises in the worst case. It turns out that you can end up effectively putting any expression into a switch case, and so you can put all kinds of branches via use of conditionals (and in modern GML, functions, but those are easier to work with). There ends up being a short algorithm to follow the cases backwards from the last case, until we know there's no more cases to be found.

Then, there's just a lot to deal with in terms of rerouting control flow and inserting new nodes. I won't get into the details here, for now.

#### But what about the remaining `continue` statements that we didn't detect?

Now's the time to resolve those remaining `continue` statements! Process of elimination style, of course. All remaining `Opcode.Branch` instructions should be guaranteed to be inside of a loop, and they should also be guaranteed to be `continue`, because we have already detected all possible `break` statements. Similarly to before, `while`/`for` loops can switch between each other here, depending on the circumstance.

### Cleaning up control flow going into end of `try` statements

This is a final and small step. It simply removes all branches that go into the successor of a `try` statement (which are not the `try` statement itself). This deals with cases where certain types of control flow are placed at the end of a `try` block, when there is only a `finally`, and no `catch` block. This is done at the end so as to not interfere with other branches getting resolved properly.

## AST steps

TODO: Need to document this. Generally, this is split into flattening out control flow into tree/block levels, and then simulating the VM stack.

## Cleanup steps

TODO: Need to document this. Overall, this is just a few passes over the syntax tree, making small modifications to make code more readable and correct.
