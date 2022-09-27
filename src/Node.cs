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

public abstract class Node : IComparable<Node> {
	public Node Parent { get; protected set; } = null;
	public List<Node> Children { get; protected set; }

	public abstract int FixedChildCount { get; }
	public bool IsVariableLength { get { return FixedChildCount == -1; } }
	protected abstract string EvaluateString();

	public int CompareTo(Node other) {
		string thisStr = this.ToString().Replace(" ", "").Replace("!", "");
		string otherStr = other.ToString().Replace(" ", "").Replace("!", "");

		return thisStr.CompareTo(otherStr);
	}

	bool isDirty;
	string cachedString;

	public static implicit operator Node(string value) {
		return new ValueNode(value);
	}

	public static implicit operator Node(char value) {
		return new ValueNode(value.ToString());
	}

	public static implicit operator Node(int value) {
		return new ValueNode(value.ToString());
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

		SetDirty();
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
			for (int i = 0; i < nodeString.Length; i++) {
				if (nodeString[i] == '(') {
					int length = GetLengthOSinglefNodeStringValueAtIndex(nodeString, i);
					
					vars.Add(Node.CreateTreeFromString(nodeString.Substring(i + 1, length - 2)));
					nodeString = nodeString.Remove(i, length).Insert(i, $"${vars.Count() - 1}$");

					Console.WriteLine(nodeString);
					break;
				}
			}
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

				int length = GetLengthOSinglefNodeStringValueAtIndex(nodeString, i + 1) + 1;
				NotNode notNode = new NotNode(EvaluateSingleNodeStringValue(nodeString.Substring(i + 1, length - 1), vars));
				vars.Add(notNode);

				nodeString = nodeString.Remove(i, length).Insert(i, $"${vars.Count() - 1}$");
				Console.WriteLine(nodeString);
			}
		}

		// And
		Console.WriteLine("-- AND --");

		while (true) {
			bool foundAnd = false;
			int startIndex = 0;
			int endIndex = 0;

			for (int i = 0; i < nodeString.Length - 1; i++) {
				if (nodeString[i] == '+' || nodeString[i + 1] == '+'
					|| (nodeString[i] == '$' && (i >= nodeString.Length - 2 || nodeString[i + 2] == '+'))
				) {
					if (!foundAnd) continue;
					else {
						if (nodeString[i] == '$') endIndex = i + 1;
						else endIndex = 1;

						break;
					}
				}

				if (!foundAnd) startIndex = i;
				foundAnd = true;
			}

			if (!foundAnd) break;

			string subStr = nodeString.Substring(startIndex, endIndex - startIndex + 1);
			nodeString = nodeString.Remove(startIndex, endIndex - startIndex + 1).Insert(startIndex, $"${vars.Count()}");
			List<Node> children = new List<Node>();

			for (int i = 0; i < subStr.Length; i++) { 
				if (subStr[i] != '$') {
					children.Add(subStr[i]);
					continue;
				}

				int varIndex = int.Parse(subStr[i + 1].ToString());
				children.Add(vars[varIndex]);
				i++;
			}

			Console.WriteLine(nodeString);
			AndNode andNode = new AndNode(children.ToArray());
			vars.Add(andNode);
		}

		Console.WriteLine("-- OR --");

		Console.WriteLine($"Created Node Tree From String \"{nodeStringOG}\"");
		return vars.Last();
	}

	static Node EvaluateSingleNodeStringValue(string nodeValue, List<Node> vars) {
		if (nodeValue[0] == '$') {
			return vars[int.Parse(nodeValue.Substring(1, nodeValue.Length - 1))];
		}

		if (nodeValue.Length > 1) {
			throw new Exception("Failed To Evaluate Single Value \"{nodeValue}\": Value Too Long");
		}

		if (nodeValue == "+" || nodeValue == "!" || nodeValue == "(" || nodeValue == ")") {
			throw new Exception("Failed To Evaluate Single Value \"{nodeValue}\": Invalid Value");
		}

		return nodeValue;
	}

	static int GetLengthOSinglefNodeStringValueAtIndex(string nodeString, int i) {
		if (i < 0 || i >= nodeString.Length) return 0;
		int startingIndex = i;

		if (nodeString[i] == '$') {
			for (i++; i < nodeString.Length; i++) {
				if (nodeString[i] == '$') return i - startingIndex + 1;
			}

			throw new Exception($"Invalid Node String \"{nodeString}\", Couldn't find closing dollar for dollar at index {startingIndex}");
		}
		
		if (nodeString[i] == '(') {
			int bracketLevel = 1;

			for (i++; i < nodeString.Length; i++) {
				if (nodeString[i] == '(') {
					bracketLevel++;
					continue;
				}

				if (nodeString[i] == ')') {
					bracketLevel--;
					if (bracketLevel == 0) return i - startingIndex + 1;
				}
			}

			throw new Exception($"Invalid Node String \"{nodeString}\", Couldn't find closing bracket for bracket at index {startingIndex}");
		}

		return 1;
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

	public void SetDirty() {
		isDirty = true;
		if (Parent != null) Parent.SetDirty();
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

		SetDirty();
	}

	public void ReplaceSelfWithNode(Node nodeToBecome) {
		nodeToBecome.Parent = Parent;
		if (Parent != null) Parent.ReplaceChild(this, nodeToBecome);

		Parent = null;
		Children.Select(child => (Node)null);
		SetDirty();
	}
}

// 	