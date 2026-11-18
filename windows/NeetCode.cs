namespace Dialed;

/// One NeetCode 150 problem. Slug is the LeetCode URL slug and the stored identifier.
public record NCProblem(string Name, string Slug, Difficulty Difficulty)
{
    public string Url => $"https://leetcode.com/problems/{Slug}/";
}

/// A NeetCode category. Topic is the matching entry in Topics.List.
public record NCSection(string Name, string Topic, NCProblem[] Problems);

public static class NeetCode
{
    public const string Blind75Key = "blind75";
    public const string NeetCode150Key = "neetcode150";
    public static readonly (string Key, string Title)[] Lists = [(Blind75Key, "blind 75"), (NeetCode150Key, "neetcode 150")];

    public static bool Contains(string list, NCProblem p) => list != Blind75Key || Blind75.Contains(p.Slug);

    public static List<NCSection> SectionsOf(string list) =>
        Sections.Select(s => s with { Problems = s.Problems.Where(p => Contains(list, p)).ToArray() })
                .Where(s => s.Problems.Length > 0).ToList();

    public static List<NCProblem> ProblemsOf(string list) => All.Where(p => Contains(list, p)).ToList();

    public static NCProblem? Problem(string? slug) => slug == null ? null : All.FirstOrDefault(p => p.Slug == slug);

    public static string TopicOf(string slug) =>
        Sections.FirstOrDefault(s => s.Problems.Any(p => p.Slug == slug))?.Topic ?? "other";

    /// Problems in the Blind 75 (from neetcode-gh/leetcode .problemSiteData.json).
    public static readonly HashSet<string> Blind75 =
    [
        "contains-duplicate", "valid-anagram", "two-sum",
        "group-anagrams", "top-k-frequent-elements", "product-of-array-except-self",
        "encode-and-decode-strings", "longest-consecutive-sequence", "valid-palindrome",
        "3sum", "container-with-most-water", "best-time-to-buy-and-sell-stock",
        "longest-substring-without-repeating-characters", "longest-repeating-character-replacement", "minimum-window-substring",
        "valid-parentheses", "find-minimum-in-rotated-sorted-array", "search-in-rotated-sorted-array",
        "reverse-linked-list", "merge-two-sorted-lists", "reorder-list",
        "remove-nth-node-from-end-of-list", "linked-list-cycle", "merge-k-sorted-lists",
        "invert-binary-tree", "maximum-depth-of-binary-tree", "same-tree",
        "subtree-of-another-tree", "lowest-common-ancestor-of-a-binary-search-tree", "binary-tree-level-order-traversal",
        "validate-binary-search-tree", "kth-smallest-element-in-a-bst", "construct-binary-tree-from-preorder-and-inorder-traversal",
        "binary-tree-maximum-path-sum", "serialize-and-deserialize-binary-tree", "implement-trie-prefix-tree",
        "design-add-and-search-words-data-structure", "word-search-ii", "find-median-from-data-stream",
        "combination-sum", "word-search", "number-of-islands",
        "clone-graph", "pacific-atlantic-water-flow", "course-schedule",
        "number-of-connected-components-in-an-undirected-graph", "graph-valid-tree", "alien-dictionary",
        "climbing-stairs", "house-robber", "house-robber-ii",
        "longest-palindromic-substring", "palindromic-substrings", "decode-ways",
        "coin-change", "maximum-product-subarray", "word-break",
        "longest-increasing-subsequence", "unique-paths", "longest-common-subsequence",
        "maximum-subarray", "jump-game", "insert-interval",
        "merge-intervals", "non-overlapping-intervals", "meeting-rooms",
        "meeting-rooms-ii", "rotate-image", "spiral-matrix",
        "set-matrix-zeroes", "number-of-1-bits", "counting-bits",
        "reverse-bits", "missing-number", "sum-of-two-integers",
    ];

    public static readonly NCSection[] Sections =
    [
        new("arrays & hashing", "arrays/hashing", [
            new("Contains Duplicate", "contains-duplicate", Difficulty.Easy),
            new("Valid Anagram", "valid-anagram", Difficulty.Easy),
            new("Two Sum", "two-sum", Difficulty.Easy),
            new("Group Anagrams", "group-anagrams", Difficulty.Medium),
            new("Top K Frequent Elements", "top-k-frequent-elements", Difficulty.Medium),
            new("Encode and Decode Strings", "encode-and-decode-strings", Difficulty.Medium),
            new("Product of Array Except Self", "product-of-array-except-self", Difficulty.Medium),
            new("Valid Sudoku", "valid-sudoku", Difficulty.Medium),
            new("Longest Consecutive Sequence", "longest-consecutive-sequence", Difficulty.Medium),
        ]),
        new("two pointers", "two pointers", [
            new("Valid Palindrome", "valid-palindrome", Difficulty.Easy),
            new("Two Sum II", "two-sum-ii-input-array-is-sorted", Difficulty.Medium),
            new("3Sum", "3sum", Difficulty.Medium),
            new("Container With Most Water", "container-with-most-water", Difficulty.Medium),
            new("Trapping Rain Water", "trapping-rain-water", Difficulty.Hard),
        ]),
        new("sliding window", "sliding window", [
            new("Best Time to Buy and Sell Stock", "best-time-to-buy-and-sell-stock", Difficulty.Easy),
            new("Longest Substring Without Repeating Characters", "longest-substring-without-repeating-characters", Difficulty.Medium),
            new("Longest Repeating Character Replacement", "longest-repeating-character-replacement", Difficulty.Medium),
            new("Permutation in String", "permutation-in-string", Difficulty.Medium),
            new("Minimum Window Substring", "minimum-window-substring", Difficulty.Hard),
            new("Sliding Window Maximum", "sliding-window-maximum", Difficulty.Hard),
        ]),
        new("stack", "stack", [
            new("Valid Parentheses", "valid-parentheses", Difficulty.Easy),
            new("Min Stack", "min-stack", Difficulty.Medium),
            new("Evaluate Reverse Polish Notation", "evaluate-reverse-polish-notation", Difficulty.Medium),
            new("Generate Parentheses", "generate-parentheses", Difficulty.Medium),
            new("Daily Temperatures", "daily-temperatures", Difficulty.Medium),
            new("Car Fleet", "car-fleet", Difficulty.Medium),
            new("Largest Rectangle in Histogram", "largest-rectangle-in-histogram", Difficulty.Hard),
        ]),
        new("binary search", "binary search", [
            new("Binary Search", "binary-search", Difficulty.Easy),
            new("Search a 2D Matrix", "search-a-2d-matrix", Difficulty.Medium),
            new("Koko Eating Bananas", "koko-eating-bananas", Difficulty.Medium),
            new("Find Minimum in Rotated Sorted Array", "find-minimum-in-rotated-sorted-array", Difficulty.Medium),
            new("Search in Rotated Sorted Array", "search-in-rotated-sorted-array", Difficulty.Medium),
            new("Time Based Key-Value Store", "time-based-key-value-store", Difficulty.Medium),
            new("Median of Two Sorted Arrays", "median-of-two-sorted-arrays", Difficulty.Hard),
        ]),
        new("linked list", "linked list", [
            new("Reverse Linked List", "reverse-linked-list", Difficulty.Easy),
            new("Merge Two Sorted Lists", "merge-two-sorted-lists", Difficulty.Easy),
            new("Linked List Cycle", "linked-list-cycle", Difficulty.Easy),
            new("Reorder List", "reorder-list", Difficulty.Medium),
            new("Remove Nth Node From End of List", "remove-nth-node-from-end-of-list", Difficulty.Medium),
            new("Copy List with Random Pointer", "copy-list-with-random-pointer", Difficulty.Medium),
            new("Add Two Numbers", "add-two-numbers", Difficulty.Medium),
            new("Find the Duplicate Number", "find-the-duplicate-number", Difficulty.Medium),
            new("LRU Cache", "lru-cache", Difficulty.Medium),
            new("Merge K Sorted Lists", "merge-k-sorted-lists", Difficulty.Hard),
            new("Reverse Nodes in K-Group", "reverse-nodes-in-k-group", Difficulty.Hard),
        ]),
        new("trees", "trees", [
            new("Invert Binary Tree", "invert-binary-tree", Difficulty.Easy),
            new("Maximum Depth of Binary Tree", "maximum-depth-of-binary-tree", Difficulty.Easy),
            new("Diameter of Binary Tree", "diameter-of-binary-tree", Difficulty.Easy),
            new("Balanced Binary Tree", "balanced-binary-tree", Difficulty.Easy),
            new("Same Tree", "same-tree", Difficulty.Easy),
            new("Subtree of Another Tree", "subtree-of-another-tree", Difficulty.Easy),
            new("Lowest Common Ancestor of a BST", "lowest-common-ancestor-of-a-binary-search-tree", Difficulty.Medium),
            new("Binary Tree Level Order Traversal", "binary-tree-level-order-traversal", Difficulty.Medium),
            new("Binary Tree Right Side View", "binary-tree-right-side-view", Difficulty.Medium),
            new("Count Good Nodes in Binary Tree", "count-good-nodes-in-binary-tree", Difficulty.Medium),
            new("Validate Binary Search Tree", "validate-binary-search-tree", Difficulty.Medium),
            new("Kth Smallest Element in a BST", "kth-smallest-element-in-a-bst", Difficulty.Medium),
            new("Construct Tree from Preorder and Inorder", "construct-binary-tree-from-preorder-and-inorder-traversal", Difficulty.Medium),
            new("Binary Tree Maximum Path Sum", "binary-tree-maximum-path-sum", Difficulty.Hard),
            new("Serialize and Deserialize Binary Tree", "serialize-and-deserialize-binary-tree", Difficulty.Hard),
        ]),
        new("tries", "tries", [
            new("Implement Trie", "implement-trie-prefix-tree", Difficulty.Medium),
            new("Design Add and Search Words", "design-add-and-search-words-data-structure", Difficulty.Medium),
            new("Word Search II", "word-search-ii", Difficulty.Hard),
        ]),
        new("heap / priority queue", "heap", [
            new("Kth Largest Element in a Stream", "kth-largest-element-in-a-stream", Difficulty.Easy),
            new("Last Stone Weight", "last-stone-weight", Difficulty.Easy),
            new("K Closest Points to Origin", "k-closest-points-to-origin", Difficulty.Medium),
            new("Kth Largest Element in an Array", "kth-largest-element-in-an-array", Difficulty.Medium),
            new("Task Scheduler", "task-scheduler", Difficulty.Medium),
            new("Design Twitter", "design-twitter", Difficulty.Medium),
            new("Find Median from Data Stream", "find-median-from-data-stream", Difficulty.Hard),
        ]),
        new("backtracking", "backtracking", [
            new("Subsets", "subsets", Difficulty.Medium),
            new("Combination Sum", "combination-sum", Difficulty.Medium),
            new("Combination Sum II", "combination-sum-ii", Difficulty.Medium),
            new("Permutations", "permutations", Difficulty.Medium),
            new("Subsets II", "subsets-ii", Difficulty.Medium),
            new("Word Search", "word-search", Difficulty.Medium),
            new("Palindrome Partitioning", "palindrome-partitioning", Difficulty.Medium),
            new("Letter Combinations of a Phone Number", "letter-combinations-of-a-phone-number", Difficulty.Medium),
            new("N-Queens", "n-queens", Difficulty.Hard),
        ]),
        new("graphs", "graphs", [
            new("Number of Islands", "number-of-islands", Difficulty.Medium),
            new("Max Area of Island", "max-area-of-island", Difficulty.Medium),
            new("Clone Graph", "clone-graph", Difficulty.Medium),
            new("Walls and Gates", "walls-and-gates", Difficulty.Medium),
            new("Rotting Oranges", "rotting-oranges", Difficulty.Medium),
            new("Pacific Atlantic Water Flow", "pacific-atlantic-water-flow", Difficulty.Medium),
            new("Surrounded Regions", "surrounded-regions", Difficulty.Medium),
            new("Course Schedule", "course-schedule", Difficulty.Medium),
            new("Course Schedule II", "course-schedule-ii", Difficulty.Medium),
            new("Graph Valid Tree", "graph-valid-tree", Difficulty.Medium),
            new("Number of Connected Components", "number-of-connected-components-in-an-undirected-graph", Difficulty.Medium),
            new("Redundant Connection", "redundant-connection", Difficulty.Medium),
            new("Word Ladder", "word-ladder", Difficulty.Hard),
        ]),
        new("advanced graphs", "graphs", [
            new("Network Delay Time", "network-delay-time", Difficulty.Medium),
            new("Reconstruct Itinerary", "reconstruct-itinerary", Difficulty.Hard),
            new("Min Cost to Connect All Points", "min-cost-to-connect-all-points", Difficulty.Medium),
            new("Swim in Rising Water", "swim-in-rising-water", Difficulty.Hard),
            new("Alien Dictionary", "alien-dictionary", Difficulty.Hard),
            new("Cheapest Flights Within K Stops", "cheapest-flights-within-k-stops", Difficulty.Medium),
        ]),
        new("1-d dp", "dp", [
            new("Climbing Stairs", "climbing-stairs", Difficulty.Easy),
            new("Min Cost Climbing Stairs", "min-cost-climbing-stairs", Difficulty.Easy),
            new("House Robber", "house-robber", Difficulty.Medium),
            new("House Robber II", "house-robber-ii", Difficulty.Medium),
            new("Longest Palindromic Substring", "longest-palindromic-substring", Difficulty.Medium),
            new("Palindromic Substrings", "palindromic-substrings", Difficulty.Medium),
            new("Decode Ways", "decode-ways", Difficulty.Medium),
            new("Coin Change", "coin-change", Difficulty.Medium),
            new("Maximum Product Subarray", "maximum-product-subarray", Difficulty.Medium),
            new("Word Break", "word-break", Difficulty.Medium),
            new("Longest Increasing Subsequence", "longest-increasing-subsequence", Difficulty.Medium),
            new("Partition Equal Subset Sum", "partition-equal-subset-sum", Difficulty.Medium),
        ]),
        new("2-d dp", "dp", [
            new("Unique Paths", "unique-paths", Difficulty.Medium),
            new("Longest Common Subsequence", "longest-common-subsequence", Difficulty.Medium),
            new("Buy and Sell Stock with Cooldown", "best-time-to-buy-and-sell-stock-with-cooldown", Difficulty.Medium),
            new("Coin Change II", "coin-change-ii", Difficulty.Medium),
            new("Target Sum", "target-sum", Difficulty.Medium),
            new("Interleaving String", "interleaving-string", Difficulty.Medium),
            new("Longest Increasing Path in a Matrix", "longest-increasing-path-in-a-matrix", Difficulty.Hard),
            new("Distinct Subsequences", "distinct-subsequences", Difficulty.Hard),
            new("Edit Distance", "edit-distance", Difficulty.Medium),
            new("Burst Balloons", "burst-balloons", Difficulty.Hard),
            new("Regular Expression Matching", "regular-expression-matching", Difficulty.Hard),
        ]),
        new("greedy", "greedy", [
            new("Maximum Subarray", "maximum-subarray", Difficulty.Medium),
            new("Jump Game", "jump-game", Difficulty.Medium),
            new("Jump Game II", "jump-game-ii", Difficulty.Medium),
            new("Gas Station", "gas-station", Difficulty.Medium),
            new("Hand of Straights", "hand-of-straights", Difficulty.Medium),
            new("Merge Triplets to Form Target Triplet", "merge-triplets-to-form-target-triplet", Difficulty.Medium),
            new("Partition Labels", "partition-labels", Difficulty.Medium),
            new("Valid Parenthesis String", "valid-parenthesis-string", Difficulty.Medium),
        ]),
        new("intervals", "intervals", [
            new("Insert Interval", "insert-interval", Difficulty.Medium),
            new("Merge Intervals", "merge-intervals", Difficulty.Medium),
            new("Non-overlapping Intervals", "non-overlapping-intervals", Difficulty.Medium),
            new("Meeting Rooms", "meeting-rooms", Difficulty.Easy),
            new("Meeting Rooms II", "meeting-rooms-ii", Difficulty.Medium),
            new("Minimum Interval to Include Each Query", "minimum-interval-to-include-each-query", Difficulty.Hard),
        ]),
        new("math & geometry", "math/bit", [
            new("Rotate Image", "rotate-image", Difficulty.Medium),
            new("Spiral Matrix", "spiral-matrix", Difficulty.Medium),
            new("Set Matrix Zeroes", "set-matrix-zeroes", Difficulty.Medium),
            new("Happy Number", "happy-number", Difficulty.Easy),
            new("Plus One", "plus-one", Difficulty.Easy),
            new("Pow(x, n)", "powx-n", Difficulty.Medium),
            new("Multiply Strings", "multiply-strings", Difficulty.Medium),
            new("Detect Squares", "detect-squares", Difficulty.Medium),
        ]),
        new("bit manipulation", "math/bit", [
            new("Single Number", "single-number", Difficulty.Easy),
            new("Number of 1 Bits", "number-of-1-bits", Difficulty.Easy),
            new("Counting Bits", "counting-bits", Difficulty.Easy),
            new("Reverse Bits", "reverse-bits", Difficulty.Easy),
            new("Missing Number", "missing-number", Difficulty.Easy),
            new("Sum of Two Integers", "sum-of-two-integers", Difficulty.Medium),
            new("Reverse Integer", "reverse-integer", Difficulty.Medium),
        ]),
    ];

    public static readonly NCProblem[] All = Sections.SelectMany(s => s.Problems).ToArray();
}
