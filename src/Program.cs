class Program {
	static void Main() {
		Node node = Node.CreateTreeFromString("(A + C)(AB + A!B) + !B(!B + C) + ((B + C + D) + A + A + C + (B) + B + (A)(B) + C + A + X + F + D + A + A + A + A + A + A)");
		// Node node = Node.CreateTreeFromString("C!B + A + B + !AB!C");

		Console.WriteLine($"Node: {node}");
		Console.WriteLine($"\nDebug Node: {node.ToDebugString()}");
	}
}