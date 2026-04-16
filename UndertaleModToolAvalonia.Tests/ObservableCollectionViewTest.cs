using System.Collections.ObjectModel;

namespace UndertaleModToolAvalonia.Tests;

public class ObservableCollectionViewTest
{
    [Fact]
    public void Test_ObservableCollectionView()
    {
        var input = new ObservableCollection<string>();
        var view = new ObservableCollectionView<string, string>(input, null, null);
        var output = view.Output;

        // Basics
        input.Add("a");
        Assert.Equal(output, ["a"]);

        input.Insert(0, "b");
        input.Add("c");
        Assert.Equal(output, ["b", "a", "c"]);

        input[1] = "d";
        Assert.Equal(output, ["b", "d", "c"]);

        input.Move(2, 0);
        Assert.Equal(output, ["c", "b", "d"]);

        input.Move(0, 2);
        Assert.Equal(output, ["b", "d", "c"]);

        input.Remove("d");
        Assert.Equal(output, ["b", "c"]);

        input.Clear();
        Assert.Equal(output, []);

        // Filter
        input.Add("A");
        input.Add("B");
        input.Add("C");
        input.Add("D");
        input.Add("E");
        Assert.Equal(output, ["A", "B", "C", "D", "E"]);

        view.SetFilter(x => x == "A");
        Assert.Equal(output, ["A"]);

        view.SetFilter(x => x == "E");
        Assert.Equal(output, ["E"]);

        view.SetFilter(x => true);
        Assert.Equal(output, ["A", "B", "C", "D", "E"]);

        // Moving while filtered
        // Yes old, yes new
        input.Move(1, 3);
        Assert.Equal(output, ["A", "C", "D", "B", "E"]);

        input.Move(3, 1);
        Assert.Equal(output, ["A", "B", "C", "D", "E"]);

        // No old, yes new
        view.SetFilter(x => x != "B");
        input.Move(1, 3);
        Assert.Equal(output, ["A", "C", "D", "E"]);

        input.Move(3, 1);
        Assert.Equal(output, ["A", "C", "D", "E"]);

        // Yes old, no new
        view.SetFilter(x => x != "D");
        input.Move(1, 3);
        Assert.Equal(output, ["A", "C", "B", "E"]);

        input.Move(3, 1);
        Assert.Equal(output, ["A", "B", "C", "E"]);

        // No old, no new
        view.SetFilter(x => x != "B" && x != "D");
        input.Move(1, 3);
        Assert.Equal(output, ["A", "C", "E"]);

        input.Move(3, 1);
        Assert.Equal(output, ["A", "C", "E"]);

        // Add while filtered and passing
        view.SetFilter(x => x == "B");
        input.Add("B");
        Assert.Equal(output, ["B", "B"]);

        // Add while filtered and not passing
        input.Add("F");
        Assert.Equal(output, ["B", "B"]);

        view.SetFilter(x => true);
        Assert.Equal(output, ["A", "B", "C", "D", "E", "B", "F"]);

        // Remove while filtered and passing
        view.SetFilter(x => x == "B");
        input.RemoveAt(5);
        Assert.Equal(output, ["B"]);

        // Remove while filtered and not passing
        input.Remove("F");
        Assert.Equal(output, ["B"]);

        view.SetFilter(x => true);
        Assert.Equal(output, ["A", "B", "C", "D", "E"]);

        // Replace yes old, yes new
        view.SetFilter(x => x == "B" || x == "b");
        input[1] = "b";
        Assert.Equal(output, ["b"]);

        // Replace yes old, no new
        view.SetFilter(x => x == "b");
        input[1] = "B";
        Assert.Equal(output, []);

        // Replace no old, yes new
        input[1] = "b";
        Assert.Equal(output, ["b"]);

        // Replace no old, no new
        view.SetFilter(x => x == "C");
        input[1] = "B";
        Assert.Equal(output, ["C"]);

        view.SetFilter(x => true);
        Assert.Equal(output, ["A", "B", "C", "D", "E"]);
    }
}
