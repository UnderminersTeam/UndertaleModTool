/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class DecompileContext_DecompileToString_GameContext
{
    [Fact]
    public void TestRepeatOldBytecode()
    {
        // Goal of this test is to not get misdetected as short-circuits and cause problems (as per a previous bug)
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 5
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [2]

            :[1]
            pushi.e 1
            pop.v.i self.a
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[2]
            popz.i
            pushi.e 5
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [4]

            :[3]
            pushi.e 1
            pop.v.i self.b
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [3]

            :[4]
            popz.i
            """,
            """
            repeat (5)
            {
                a = 1;
            }
            repeat (5)
            {
                b = 1;
            }
            """,
            new GameContextMock()
            {
                Bytecode14OrLower = true,
                UsingGMLv2 = false,
                UsingGMS2OrLater = false
            }
        );
    }
}
