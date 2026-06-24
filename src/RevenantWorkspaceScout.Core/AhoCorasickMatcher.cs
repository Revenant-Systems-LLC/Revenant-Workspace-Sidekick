namespace RevenantWorkspaceScout.Core;

/// <summary>
/// O(N) multi-pattern literal string search — immune to ReDoS.
/// Implements Aho-Corasick with failure links via BFS.
/// </summary>
public sealed class AhoCorasickMatcher
{
    private sealed class Node
    {
        public readonly Dictionary<char, Node> Children = new();
        public Node? Fail;
        public readonly List<int> Outputs = [];
    }

    private readonly Node _root = new();
    private readonly string[] _patterns;

    public AhoCorasickMatcher(IReadOnlyList<string> patterns)
    {
        _patterns = [.. patterns];

        for (var i = 0; i < _patterns.Length; i++)
        {
            var node = _root;
            foreach (var c in _patterns[i])
            {
                if (!node.Children.TryGetValue(c, out var child))
                    node.Children[c] = child = new Node();
                node = child;
            }
            node.Outputs.Add(i);
        }

        BuildFailureLinks();
    }

    private void BuildFailureLinks()
    {
        var queue = new Queue<Node>();
        foreach (var child in _root.Children.Values)
        {
            child.Fail = _root;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            foreach (var (key, child) in curr.Children)
            {
                queue.Enqueue(child);

                var fail = curr.Fail;
                while (fail is not null && !fail.Children.ContainsKey(key))
                    fail = fail.Fail;

                child.Fail = fail is not null && fail.Children.TryGetValue(key, out var failChild)
                    ? failChild
                    : _root;

                if (child.Fail != child)
                    child.Outputs.AddRange(child.Fail.Outputs);
            }
        }
    }

    /// <summary>Returns (patternIndex, startInclusive, endExclusive) for every match.</summary>
    public IEnumerable<(int PatternIndex, int Start, int End)> Search(string text)
    {
        var node = _root;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            while (node != _root && !node.Children.ContainsKey(c))
                node = node.Fail!;

            if (node.Children.TryGetValue(c, out var next))
                node = next;

            foreach (var idx in node.Outputs)
            {
                var pat = _patterns[idx];
                var start = i - pat.Length + 1;
                yield return (idx, start, i + 1);
            }
        }
    }

    public string GetPattern(int index) => _patterns[index];
}
