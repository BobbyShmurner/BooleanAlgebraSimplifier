class Program {
	static void Main() {
		//Node node = Node.CreateTreeFromString("(A + C)(AB + A!B) + !B(!B + C)");
		Node node = Node.CreateTreeFromString("C!B + A + B + !AB!C");

		Console.WriteLine($"Node: {node}");
	}
}