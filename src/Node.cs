public class AndNode : Node {
	public AndNode(params Node[] children) : base(children) {}
	public override int FixedChildCount { get { return -1; } }

	protected override string EvaluateString() {
		Children.Sort();

		string evaluatedString = "";
		foreach (Node child in Children) {
			if (child is OrNode) evaluatedString += $"({child})";
			else evaluatedString += $"{child}";
		}

		return evaluatedString;
	}
}

public class OrNode : Node {
	public OrNode(params Node[] children) : base(children) {}
	public override int FixedChildCount { get { return -1; } }

	protected override string EvaluateString() {
		Children.Sort();

		string evaluatedString = "";
		foreach (Node child in Children) {
			evaluatedString += $"{child} + ";
		}

		return evaluatedString.Substring(0, evaluatedString.Length - 3);
	}
}

public class NotNode : Node {
	public NotNode(Node child) : base(child) {}
	public override int FixedChildCount { get { return 1; } }
	
	protected override string EvaluateString() {
		Node child = Children[0];

		if (child is ValueNode) {
			return $"!{child}";
		} else {
			return $"!({child})";
		}
	}
}

public class ValueNode : Node {
	public string Value { get; private set; }
	public override int FixedChildCount { get { return 0; } }

	protected override string EvaluateString() {
		return Value;
	}

	public ValueNode(string value) : base() {
		Value = value;
	}
}

public abstract class Node {
	public Node Parent { get; protected set; } = null;
	public List<Node> Children { get; protected set; }

	public abstract int FixedChildCount { get; }
	public bool IsVariableLength { get { return FixedChildCount == -1; } }
	protected abstract string EvaluateString();

	bool isDirty;
	string cachedString;

	public static implicit operator Node(string value) {
		return new ValueNode(value);
	}

	public Node(params Node[] children) {
		if (IsVariableLength) {
			Children = new List<Node>(children);
		} else {
			Children = new List<Node>();

			for (int i = 0; i < children.Length && i < FixedChildCount; i++) {
				Children.Add(children[i]);
			}
		}

		isDirty = true;
	}

	public static Node CreateTreeFromString(string nodeString) {
		string nodeStringOG = nodeString;

		nodeString = nodeString.Replace(" ", "");
		nodeString = nodeString.Replace(".", "");
		List<Node> vars = new List<Node>();

		Console.WriteLine($"Creating Node Tree From String \"{nodeStringOG}\"");

		// Brackets
		if (nodeString.Count((c) => c == '(') != nodeString.Count((c) => c == ')')) {
			throw new Exception("Failed to Parse Node String \"{nodeStringOG}\": The amount of opening and closing brackets are not equal");
		}

		Console.WriteLine("-- Brackets --");

		while (nodeString.Count((c) => c == '(') > 0) {
			int bracketLayer = 0;
			int startIndex = 0;
			int endIndex = 0;

			for (int i = 0; i < nodeString.Length; i++) {
				char c = nodeString[i];

				if (c == '(') {
					bracketLayer++;
					startIndex = i;
					continue;
				}

				if (c == ')') {
					bracketLayer--;

					if (bracketLayer == 0) {
						endIndex = i;
						break;
					}
				}
			}

			vars.Add(Node.CreateTreeFromString(nodeString.Substring(startIndex + 1, endIndex - startIndex - 1)));
			nodeString = nodeString.Remove(startIndex, endIndex - startIndex + 1).Insert(startIndex, $"${vars.Count() - 1}");
			Console.WriteLine(nodeString);
		}

		// Not
		Console.WriteLine("-- NOT --");

		while (nodeString.Count((c) => c == '!') > 0) {
			for (int i = 0; i < nodeString.Length; i++) {
				if (nodeString[i] != '!') continue;
				if (nodeString[i + 1] == '!') {
					nodeString = nodeString.Remove(i, 2);
					i--;

					continue;
				}

				NotNode notNode = null;

				if (nodeString[i + 1] == '$') {
					int varIndex = int.Parse(nodeString[i + 2].ToString());
					notNode = new NotNode(vars[varIndex]);

					nodeString = nodeString.Remove(i, 3).Insert(i, $"${vars.Count()}");
				} else {
					notNode = new NotNode(nodeString[i + 1].ToString());
					nodeString = nodeString.Remove(i, 2).Insert(i, $"${vars.Count()}");
				}

				vars.Add(notNode);
				Console.WriteLine(nodeString);
				break;
			}
		}

		// And
		Console.WriteLine("-- AND --");

		while (true) {
			bool foundAnd = false;
			int startIndex = 0;
			int endIndex = nodeString.Length;

			for (int i = 0; i < nodeString.Length - 1; i++) {
				if (nodeString[i] == '+' || nodeString[i + 1] == '+') {
					if (!foundAnd) continue;
					else {
						endIndex = i;
						break;
					}
				}

				foundAnd = true;
				startIndex = i;
			}

			

			if (!foundAnd) break;
		}

		Console.WriteLine($"Created Node Tree From String \"{nodeStringOG}\"");
		return null;
	}

	public int GetCountOfChildType(Type type) {
		int count = 0;
		foreach (Node child in Children) {
			if (child.GetType() == type) count++;
		}

		return count;
	}

	public Node[] GetChildrenFromTypes(params Type[] types) {
		if (types.Length > Children.Count) {
			throw new Exception($"Type array cannot be larger than the child count: Child Count: {Children.Count}, Type Array Count: {types.Length}");
		}

		Node[] orderedNodes = new Node[types.Length];
		List<Node> unorderedNodes = new List<Node>(Children);

		for (int i = 0; i < types.Length; i++) {
			Type type = types[i];
			bool foundType = false;

			foreach (Node child in unorderedNodes) {
				if (child.GetType() == type) {
					foundType = true;
					orderedNodes[i] = child;
					unorderedNodes.Remove(child);

					break;
				}
			}

			if (!foundType) {
				throw new Exception($"Invalid Type Array. Type \"{type}\" at index {i} could not be matched with a node of that type");
			}
		}

		return orderedNodes;
	}

	public override string ToString() {
		if (isDirty) {
			cachedString = EvaluateString();
			isDirty = false;
		}

		return cachedString;
	}

	public override bool Equals(object obj) {
		Node other = obj as Node;
		if (other == null) return false;

		return this.ToString() == other.ToString();
	}

	public override int GetHashCode() {
		return this.ToString().GetHashCode();
	}

	public void ReplaceChild(Node child, Node newNode) {
		if (!Children.Contains(child)) {
			throw new Exception($"Node \"{this}\" doesn't contain child \"{child}\"");
		}

		Children.Select(node => {
			return node == child ? newNode : node;
		});

		isDirty = true;
	}

	public void ReplaceSelfWithNode(Node nodeToBecome) {
		nodeToBecome.Parent = Parent;
		if (Parent != null) Parent.ReplaceChild(this, nodeToBecome);

		Parent = null;
		Children.Select(child => (Node)null);
		isDirty = true;
	}
}

// 	