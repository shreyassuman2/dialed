import Foundation

/// One NeetCode 150 problem. `slug` is the LeetCode URL slug and the stored identifier.
struct NCProblem: Identifiable, Hashable {
    let name: String
    let slug: String
    let difficulty: Difficulty
    var id: String { slug }
    var url: URL { URL(string: "https://leetcode.com/problems/\(slug)/")! }
}

/// A NeetCode category. `topic` is the matching entry in `topicList`.
struct NCSection: Identifiable {
    let name: String
    let topic: String
    let problems: [NCProblem]
    var id: String { name }
}

enum NeetCode {
    static let all: [NCProblem] = sections.flatMap(\.problems)

    static func problem(_ slug: String) -> NCProblem? { all.first { $0.slug == slug } }

    static func topic(of slug: String) -> String {
        sections.first { $0.problems.contains { $0.slug == slug } }?.topic ?? "other"
    }

    private static func p(_ name: String, _ slug: String, _ d: Difficulty) -> NCProblem {
        NCProblem(name: name, slug: slug, difficulty: d)
    }

    static let sections: [NCSection] = [
        NCSection(name: "arrays & hashing", topic: "arrays/hashing", problems: [
            p("Contains Duplicate", "contains-duplicate", .easy),
            p("Valid Anagram", "valid-anagram", .easy),
            p("Two Sum", "two-sum", .easy),
            p("Group Anagrams", "group-anagrams", .medium),
            p("Top K Frequent Elements", "top-k-frequent-elements", .medium),
            p("Encode and Decode Strings", "encode-and-decode-strings", .medium),
            p("Product of Array Except Self", "product-of-array-except-self", .medium),
            p("Valid Sudoku", "valid-sudoku", .medium),
            p("Longest Consecutive Sequence", "longest-consecutive-sequence", .medium),
        ]),
        NCSection(name: "two pointers", topic: "two pointers", problems: [
            p("Valid Palindrome", "valid-palindrome", .easy),
            p("Two Sum II", "two-sum-ii-input-array-is-sorted", .medium),
            p("3Sum", "3sum", .medium),
            p("Container With Most Water", "container-with-most-water", .medium),
            p("Trapping Rain Water", "trapping-rain-water", .hard),
        ]),
        NCSection(name: "sliding window", topic: "sliding window", problems: [
            p("Best Time to Buy and Sell Stock", "best-time-to-buy-and-sell-stock", .easy),
            p("Longest Substring Without Repeating Characters", "longest-substring-without-repeating-characters", .medium),
            p("Longest Repeating Character Replacement", "longest-repeating-character-replacement", .medium),
            p("Permutation in String", "permutation-in-string", .medium),
            p("Minimum Window Substring", "minimum-window-substring", .hard),
            p("Sliding Window Maximum", "sliding-window-maximum", .hard),
        ]),
        NCSection(name: "stack", topic: "stack", problems: [
            p("Valid Parentheses", "valid-parentheses", .easy),
            p("Min Stack", "min-stack", .medium),
            p("Evaluate Reverse Polish Notation", "evaluate-reverse-polish-notation", .medium),
            p("Generate Parentheses", "generate-parentheses", .medium),
            p("Daily Temperatures", "daily-temperatures", .medium),
            p("Car Fleet", "car-fleet", .medium),
            p("Largest Rectangle in Histogram", "largest-rectangle-in-histogram", .hard),
        ]),
        NCSection(name: "binary search", topic: "binary search", problems: [
            p("Binary Search", "binary-search", .easy),
            p("Search a 2D Matrix", "search-a-2d-matrix", .medium),
            p("Koko Eating Bananas", "koko-eating-bananas", .medium),
            p("Find Minimum in Rotated Sorted Array", "find-minimum-in-rotated-sorted-array", .medium),
            p("Search in Rotated Sorted Array", "search-in-rotated-sorted-array", .medium),
            p("Time Based Key-Value Store", "time-based-key-value-store", .medium),
            p("Median of Two Sorted Arrays", "median-of-two-sorted-arrays", .hard),
        ]),
        NCSection(name: "linked list", topic: "linked list", problems: [
            p("Reverse Linked List", "reverse-linked-list", .easy),
            p("Merge Two Sorted Lists", "merge-two-sorted-lists", .easy),
            p("Linked List Cycle", "linked-list-cycle", .easy),
            p("Reorder List", "reorder-list", .medium),
            p("Remove Nth Node From End of List", "remove-nth-node-from-end-of-list", .medium),
            p("Copy List with Random Pointer", "copy-list-with-random-pointer", .medium),
            p("Add Two Numbers", "add-two-numbers", .medium),
            p("Find the Duplicate Number", "find-the-duplicate-number", .medium),
            p("LRU Cache", "lru-cache", .medium),
            p("Merge K Sorted Lists", "merge-k-sorted-lists", .hard),
            p("Reverse Nodes in K-Group", "reverse-nodes-in-k-group", .hard),
        ]),
        NCSection(name: "trees", topic: "trees", problems: [
            p("Invert Binary Tree", "invert-binary-tree", .easy),
            p("Maximum Depth of Binary Tree", "maximum-depth-of-binary-tree", .easy),
            p("Diameter of Binary Tree", "diameter-of-binary-tree", .easy),
            p("Balanced Binary Tree", "balanced-binary-tree", .easy),
            p("Same Tree", "same-tree", .easy),
            p("Subtree of Another Tree", "subtree-of-another-tree", .easy),
            p("Lowest Common Ancestor of a BST", "lowest-common-ancestor-of-a-binary-search-tree", .medium),
            p("Binary Tree Level Order Traversal", "binary-tree-level-order-traversal", .medium),
            p("Binary Tree Right Side View", "binary-tree-right-side-view", .medium),
            p("Count Good Nodes in Binary Tree", "count-good-nodes-in-binary-tree", .medium),
            p("Validate Binary Search Tree", "validate-binary-search-tree", .medium),
            p("Kth Smallest Element in a BST", "kth-smallest-element-in-a-bst", .medium),
            p("Construct Tree from Preorder and Inorder", "construct-binary-tree-from-preorder-and-inorder-traversal", .medium),
            p("Binary Tree Maximum Path Sum", "binary-tree-maximum-path-sum", .hard),
            p("Serialize and Deserialize Binary Tree", "serialize-and-deserialize-binary-tree", .hard),
        ]),
        NCSection(name: "tries", topic: "tries", problems: [
            p("Implement Trie", "implement-trie-prefix-tree", .medium),
            p("Design Add and Search Words", "design-add-and-search-words-data-structure", .medium),
            p("Word Search II", "word-search-ii", .hard),
        ]),
        NCSection(name: "heap / priority queue", topic: "heap", problems: [
            p("Kth Largest Element in a Stream", "kth-largest-element-in-a-stream", .easy),
            p("Last Stone Weight", "last-stone-weight", .easy),
            p("K Closest Points to Origin", "k-closest-points-to-origin", .medium),
            p("Kth Largest Element in an Array", "kth-largest-element-in-an-array", .medium),
            p("Task Scheduler", "task-scheduler", .medium),
            p("Design Twitter", "design-twitter", .medium),
            p("Find Median from Data Stream", "find-median-from-data-stream", .hard),
        ]),
        NCSection(name: "backtracking", topic: "backtracking", problems: [
            p("Subsets", "subsets", .medium),
            p("Combination Sum", "combination-sum", .medium),
            p("Combination Sum II", "combination-sum-ii", .medium),
            p("Permutations", "permutations", .medium),
            p("Subsets II", "subsets-ii", .medium),
            p("Word Search", "word-search", .medium),
            p("Palindrome Partitioning", "palindrome-partitioning", .medium),
            p("Letter Combinations of a Phone Number", "letter-combinations-of-a-phone-number", .medium),
            p("N-Queens", "n-queens", .hard),
        ]),
        NCSection(name: "graphs", topic: "graphs", problems: [
            p("Number of Islands", "number-of-islands", .medium),
            p("Max Area of Island", "max-area-of-island", .medium),
            p("Clone Graph", "clone-graph", .medium),
            p("Walls and Gates", "walls-and-gates", .medium),
            p("Rotting Oranges", "rotting-oranges", .medium),
            p("Pacific Atlantic Water Flow", "pacific-atlantic-water-flow", .medium),
            p("Surrounded Regions", "surrounded-regions", .medium),
            p("Course Schedule", "course-schedule", .medium),
            p("Course Schedule II", "course-schedule-ii", .medium),
            p("Graph Valid Tree", "graph-valid-tree", .medium),
            p("Number of Connected Components", "number-of-connected-components-in-an-undirected-graph", .medium),
            p("Redundant Connection", "redundant-connection", .medium),
            p("Word Ladder", "word-ladder", .hard),
        ]),
        NCSection(name: "advanced graphs", topic: "graphs", problems: [
            p("Network Delay Time", "network-delay-time", .medium),
            p("Reconstruct Itinerary", "reconstruct-itinerary", .hard),
            p("Min Cost to Connect All Points", "min-cost-to-connect-all-points", .medium),
            p("Swim in Rising Water", "swim-in-rising-water", .hard),
            p("Alien Dictionary", "alien-dictionary", .hard),
            p("Cheapest Flights Within K Stops", "cheapest-flights-within-k-stops", .medium),
        ]),
        NCSection(name: "1-d dp", topic: "dp", problems: [
            p("Climbing Stairs", "climbing-stairs", .easy),
            p("Min Cost Climbing Stairs", "min-cost-climbing-stairs", .easy),
            p("House Robber", "house-robber", .medium),
            p("House Robber II", "house-robber-ii", .medium),
            p("Longest Palindromic Substring", "longest-palindromic-substring", .medium),
            p("Palindromic Substrings", "palindromic-substrings", .medium),
            p("Decode Ways", "decode-ways", .medium),
            p("Coin Change", "coin-change", .medium),
            p("Maximum Product Subarray", "maximum-product-subarray", .medium),
            p("Word Break", "word-break", .medium),
            p("Longest Increasing Subsequence", "longest-increasing-subsequence", .medium),
            p("Partition Equal Subset Sum", "partition-equal-subset-sum", .medium),
        ]),
        NCSection(name: "2-d dp", topic: "dp", problems: [
            p("Unique Paths", "unique-paths", .medium),
            p("Longest Common Subsequence", "longest-common-subsequence", .medium),
            p("Buy and Sell Stock with Cooldown", "best-time-to-buy-and-sell-stock-with-cooldown", .medium),
            p("Coin Change II", "coin-change-ii", .medium),
            p("Target Sum", "target-sum", .medium),
            p("Interleaving String", "interleaving-string", .medium),
            p("Longest Increasing Path in a Matrix", "longest-increasing-path-in-a-matrix", .hard),
            p("Distinct Subsequences", "distinct-subsequences", .hard),
            p("Edit Distance", "edit-distance", .medium),
            p("Burst Balloons", "burst-balloons", .hard),
            p("Regular Expression Matching", "regular-expression-matching", .hard),
        ]),
        NCSection(name: "greedy", topic: "greedy", problems: [
            p("Maximum Subarray", "maximum-subarray", .medium),
            p("Jump Game", "jump-game", .medium),
            p("Jump Game II", "jump-game-ii", .medium),
            p("Gas Station", "gas-station", .medium),
            p("Hand of Straights", "hand-of-straights", .medium),
            p("Merge Triplets to Form Target Triplet", "merge-triplets-to-form-target-triplet", .medium),
            p("Partition Labels", "partition-labels", .medium),
            p("Valid Parenthesis String", "valid-parenthesis-string", .medium),
        ]),
        NCSection(name: "intervals", topic: "intervals", problems: [
            p("Insert Interval", "insert-interval", .medium),
            p("Merge Intervals", "merge-intervals", .medium),
            p("Non-overlapping Intervals", "non-overlapping-intervals", .medium),
            p("Meeting Rooms", "meeting-rooms", .easy),
            p("Meeting Rooms II", "meeting-rooms-ii", .medium),
            p("Minimum Interval to Include Each Query", "minimum-interval-to-include-each-query", .hard),
        ]),
        NCSection(name: "math & geometry", topic: "math/bit", problems: [
            p("Rotate Image", "rotate-image", .medium),
            p("Spiral Matrix", "spiral-matrix", .medium),
            p("Set Matrix Zeroes", "set-matrix-zeroes", .medium),
            p("Happy Number", "happy-number", .easy),
            p("Plus One", "plus-one", .easy),
            p("Pow(x, n)", "powx-n", .medium),
            p("Multiply Strings", "multiply-strings", .medium),
            p("Detect Squares", "detect-squares", .medium),
        ]),
        NCSection(name: "bit manipulation", topic: "math/bit", problems: [
            p("Single Number", "single-number", .easy),
            p("Number of 1 Bits", "number-of-1-bits", .easy),
            p("Counting Bits", "counting-bits", .easy),
            p("Reverse Bits", "reverse-bits", .easy),
            p("Missing Number", "missing-number", .easy),
            p("Sum of Two Integers", "sum-of-two-integers", .medium),
            p("Reverse Integer", "reverse-integer", .medium),
        ]),
    ]
}
