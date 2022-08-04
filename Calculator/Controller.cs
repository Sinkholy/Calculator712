using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;


namespace Calculator712.Calculator
{
	public class Controller
	{
		const string CalculationButtonSymbol = "=";
		const string BackspaceButtonSymbol = "<-";
		const string ClearButtonSymbol = "Clear";

		ICollection<IPanel> panels;
		HistoryPanel historyPanel;
		InputPanel inputPanel;

		public PanelsHolder PanelsHolder { get; }
		ICalculatorOperation[] Operations { get; }

		#region Layout handling
		XDocument _layout;
		public XDocument Layout
		{
			get => _layout;
			set
			{
				_layout = value;
				ApplyLayout();
			}
		}
		public Action<XDocument> LayoutSaveRequested = delegate { };

		void ApplyLayout()
		{
			var panelsNode = Layout.Root.Element("panels");
			SetupDefaultPanels();
			SetupLayoutablePanels();

			var layout = new PanelsHolder.Layout()
			{
				RowsSize = int.Parse(Layout.Root.Element("size").Element("rows").Value),
				ColumnsSize = int.Parse(Layout.Root.Element("size").Element("columns").Value),
				Panels = panels
			};
			PanelsHolder.ApplyLayout(layout);

			void SetupDefaultPanels()
			{
				var historyPanelNode = panelsNode.Element("defaultPanels").Elements("panel").Single(x => x.Attribute("name").Value == "history");
				historyPanel.Position = new IPanel.PanelPosition()
				{
					Column = int.Parse(historyPanelNode.Element("position").Element("column").Value),
					Row = int.Parse(historyPanelNode.Element("position").Element("row").Value)
				};

				var inputPanelNode = panelsNode.Element("defaultPanels").Elements("panel").Single(x => x.Attribute("name").Value == "input");
				inputPanel.Position = new IPanel.PanelPosition()
				{
					Column = int.Parse(inputPanelNode.Element("position").Element("column").Value),
					Row = int.Parse(inputPanelNode.Element("position").Element("row").Value)
				};

				foreach (var panelNode in panelsNode.Elements("defaultPanels"))
				{
					// TODO: undone
				}

				panels.Add(historyPanel);
				panels.Add(inputPanel);
			}
			void SetupLayoutablePanels()
			{
				foreach (var panelNode in panelsNode.Element("layoutablePanels").Elements("panel"))
				{
					var panelName = panelNode.Attribute("name").Value;
					var panel = new GridMeshLayoutablePanel(panelName)
					{
						Position = new IPanel.PanelPosition()
						{
							Column = int.Parse(panelNode.Element("position").Element("column").Value),
							Row = int.Parse(panelNode.Element("position").Element("row").Value)
						}
					};

					panel.ButtonClicked += PanelButtonCommandRouter;
					panel.ApplyLayout(panelNode);
					panels.Add(panel);
				}
			}
		}
#endregion

		public Controller(ICalculatorOperation[] operations, XDocument layout)
		{
			historyPanel = new HistoryPanel();
			inputPanel = new InputPanel();
			panels = new List<IPanel>();
			PanelsHolder = new PanelsHolder();

			Operations = operations;
			Layout = layout;
		}

		#region Panels buttons handlers
		void PanelButtonCommandRouter(IPanel panel, Button button)
		{
			var symbol = button.Content.ToString();
			if (TryGetNumberFromSymbol(symbol, out int number))
			{
				NumericButtonClickHandler(number);
			}
			else if (TryGetOperationFromSymbol(symbol, out var operation))
			{
				OperationButtonClickHandler(operation);
			}
			else if (TryGetUtilityFromSymbol(symbol))
			{
				UtilitiesButtonClickHandler(symbol);
			}
			else
			{
				throw new ArgumentException(); // TODO: исключение
			}
		}
		bool TryGetNumberFromSymbol(string symbol, out int number)
		{
			return int.TryParse(symbol, out number);
		}
		bool TryGetOperationFromSymbol(string symbol, out ICalculatorOperation operation)
		{
			operation = Operations.SingleOrDefault(x => x.Symbol == symbol);
			return operation != null;
		}
		bool TryGetUtilityFromSymbol(string symbol)
		{
			return symbol is CalculationButtonSymbol
						  or BackspaceButtonSymbol
						  or ClearButtonSymbol;
		}
		void NumericButtonClickHandler(int number)
		{
			if (inputPanel.ContainsResult)
			{
				historyPanel.Add(inputPanel.Calculation);
				inputPanel.Clear();
			}
			inputPanel.AddToCurrentOperand(number);
		}
		void OperationButtonClickHandler(ICalculatorOperation operation)
		{
			if (!inputPanel.OperationAdded)
			{
				inputPanel.SetOperation(operation.Symbol);
			}
		}
		void UtilitiesButtonClickHandler(string symbol)
		{
			switch (symbol)
			{
				case "=":
					CalculationButtonClickHandler();
					break;
				case "<-":
					BackspaceButtonClickHandler();
					break;
				case "Clear":
					ClearButtonClickHandler();
					break;
			}

			void CalculationButtonClickHandler()
			{
				if (inputPanel.CalculationReadyToProcess)
				{
					var current = inputPanel.Calculation;
					var targetOperation = GetOperationBySymbol(current.OperationSymbol);
					var result = targetOperation.Calculate(current.LeftOperand, current.RightOperand);
					inputPanel.SetResult(result);
				}
			}
			void BackspaceButtonClickHandler()
			{
				inputPanel.Clear();
			}
			void ClearButtonClickHandler()
			{
				historyPanel.Clear();
			}
		}
		#endregion
		ICalculatorOperation GetOperationBySymbol(string symbol)
		{
			foreach(var operation in Operations)
			{
				if(operation.Symbol == symbol)
				{
					return operation;
				}
			}
			return null;
		}
		#region Panels
		internal interface IPanel
		{
			public UIElement UIElement { get; } // TODO: dafuck?
			public string Name { get; }
			public bool PositionLocked { get; set; }
			PanelPosition Position { get; set; }

			public struct PanelPosition
			{
				public int Row;
				public int Column;

				public PanelPosition(int row, int column)
				{
					Row = row;
					Column = column;
				}
			}
		}
		interface ILayoutablePanel : IPanel
		{
			public void ApplyLayout(XElement layoutSerialized);
			public XElement ExtractLayout();
		}
		abstract class GridMeshPanel : IPanel
		{
			internal Action<GridMeshPanel, Button> ButtonClicked = delegate { };

			public string Name { get; protected set; }
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			public UIElement UIElement => Mesh;
			protected GridMesh Mesh { get; private init; }

			internal GridMeshPanel(string name)
			{
				Name = name;
				Mesh = new GridMesh();
			}
		}
		class GridMeshLayoutablePanel : GridMeshPanel, ILayoutablePanel
		{
			public GridMeshLayoutablePanel(string name)
				: base(name) { }

			public virtual XElement ExtractLayout()
			{
				var sizeElem = new XElement("size",
							new XElement("columns", Mesh.ColumnsCount),
									new XElement("rows", Mesh.RowsCount));
				var positionElem = new XElement("position",
										new XElement("column", Position.Column),
										new XElement("row", Position.Row));

				var buttonsElem = new XElement("buttons");
				foreach (var cell in Mesh.Cells)
				{
					if (cell.Content is Button button)
					{
						buttonsElem.Add(new XElement("button",
															new XAttribute("symbol", button.Content),
															new XElement("position",
																new XElement("row", cell.Row),
																new XElement("column", cell.Column))));
					}
				}

				return new XElement(Name,
										sizeElem,
										positionElem,
										buttonsElem);
			}
			public virtual void ApplyLayout(XElement layoutSerialized)
			{
				var size = GetPanelSize(layoutSerialized);
				SetSize(size);
				ApplyButtonsLayout();

				void ApplyButtonsLayout()
				{
					foreach (var buttonNode in layoutSerialized.Element("buttons").Elements("button"))
					{
						var buttonSymbol = buttonNode.Attribute("symbol").Value;
						var buttonRow = int.Parse(buttonNode.Element("position").Element("row").Value);
						var buttonColumn = int.Parse(buttonNode.Element("position").Element("column").Value);

						var button = CreateButtonFromSymbol(buttonSymbol);
						button.Click += (btn, _) => ButtonClicked(this, btn as Button);

						Mesh.Pick(buttonRow, buttonColumn).Content = button;
					}
				}
			}
			protected virtual PanelSize GetPanelSize(XElement root)
			{
				return new PanelSize()
				{
					Columns = int.Parse(root.Element("size").Element("columns").Value),
					Rows = int.Parse(root.Element("size").Element("rows").Value)
				};
			}
			protected virtual void SetSize(PanelSize size)
			{
				Mesh.SetSize(size.Rows, size.Columns);
			}
			protected virtual Button CreateButtonFromSymbol(string symbol)
			{
				return new Button() { Content = symbol };
			}

			protected struct PanelSize
			{
				internal int Columns;
				internal int Rows;
			}
		}
		class InputPanel : IPanel
		{
			#region IPanel impl
			public UIElement UIElement => this;
			public string Name => "input";
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			#endregion

			Calculation calculation;
			List<int> input;
			int firstOperandNumbersCount;
			View view;

			public static implicit operator UIElement(InputPanel panel)
			{
				return panel.view;
			}

			internal InputPanel()
			{
				calculation = new Calculation();
				input = new List<int>();
				view = new View();
			}

			internal bool ContainsLeftOperand => input.Count > 0;
			internal bool ContainsRightOperand => firstOperandNumbersCount > 0;
			internal bool OperationAdded => !string.IsNullOrWhiteSpace(calculation.OperationSymbol);
			internal bool ContainsResult => calculation.Result != 0; // UNDONE: затычка
			internal bool CalculationReadyToProcess => ContainsLeftOperand && OperationAdded && ContainsRightOperand;
			internal Calculation Calculation
			{
				get
				{
					CalculateOperands();
					return calculation;
				}
			}

			internal void AddToCurrentOperand(int value)
			{
				input.Add(value);
				view.Append(value.ToString());
			}
			internal void SetOperation(string symbol)
			{
				calculation.OperationSymbol = symbol;
				firstOperandNumbersCount = input.Count;
				view.Append(symbol);
			}
			internal void SetResult(int value)
			{
				calculation.Result = value;
				view.Set(calculation.ToString());
			}
			internal void Clear()
			{
				input.Clear();
				firstOperandNumbersCount = 0;
				view.Clear();
				calculation = new Calculation();
			}
			void CalculateOperands()
			{
				calculation.LeftOperand = CalculateLeftOperand();
				calculation.RightOperand = CalculateRightOperand();

				int CalculateLeftOperand()
				{
					var sb = new StringBuilder(firstOperandNumbersCount);

					for (int i = 0; i < firstOperandNumbersCount; i++)
					{
						sb.Append(input[i]);
					}

					return int.Parse(sb.ToString());
				}
				int CalculateRightOperand()
				{
					var sb = new StringBuilder(firstOperandNumbersCount);

					for (int i = firstOperandNumbersCount; i < input.Count; i++)
					{
						sb.Append(input[i]);
					}

					return int.Parse(sb.ToString());
				}
			}

			class View
			{
				const string SpacerSymbol = " "; // TODO: спейсеры и их логика должны быть здесь?

				readonly TextBlock text;

				public static implicit operator UIElement(View view)
				{
					return view.text;
				}

				public View()
				{
					text = new TextBlock();
				}

				internal void Append(string val, bool usingSpacers = true)
				{
					if (usingSpacers)
					{
						text.Text += $"{SpacerSymbol}{val}{SpacerSymbol}";
					}
					else
					{
						text.Text += val;
					}
				}
				internal void Set(string val)
				{
					text.Text = val;
				}
				internal void Clear()
				{
					text.Text = string.Empty;
				}
			}
		}
		struct Calculation
		{
			const string EqualSign = "=";
			const string Spacer = " ";

			internal int LeftOperand { get; set; }
			internal int RightOperand { get; set; }
			internal string OperationSymbol { get; set; }
			internal int Result { get; set; }

			public override string ToString()
			{
				return $"{LeftOperand}{Spacer}{OperationSymbol}{Spacer}{RightOperand}{Spacer}{EqualSign}{Spacer}{Result}";
			}
		}
		class HistoryPanel : IPanel
		{
			readonly Archive archive;
			readonly View view;

			#region IPanel impl
			public UIElement UIElement => view;
			public string Name => "history";
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			#endregion

			public static implicit operator UIElement(HistoryPanel panel)
			{
				return panel.view;
			}

			internal HistoryPanel()
			{
				archive = new Archive();
				view = new View();
			}

			internal IReadOnlyCollection<Calculation> GetArchive()
			{
				return archive.GetFullArchive();
			}
			internal void Add(Calculation calculation)
			{
				archive.Add(calculation);
				view.Add(calculation.ToString());
			}
			internal void Clear()
			{
				archive.Clear();
				view.Clear();
			}

			class View
			{
				readonly ListBox list;

				public static implicit operator UIElement(View view)
				{
					return view.list;
				}

				internal View()
				{
					list = new ListBox();
				}

				internal void Add(string val)
				{
					var button = new Button() { Content = val };
					list.Items.Add(button);
				}
				internal void Clear()
				{
					list.Items.Clear();
				}
			}
			class Archive
			{
				readonly List<Calculation> calculations;

				internal Archive()
				{
					calculations = new List<Calculation>();
				}

				internal void Add(Calculation calculation)
				{
					calculations.Add(calculation);
				}
				internal void Clear()
				{
					calculations.Clear();
				}
				internal Calculation this[int index]
				{
					get
					{
						return calculations[index];
					}
				}
				internal IReadOnlyCollection<Calculation> GetFullArchive()
				{
					return calculations;
				}
			}
		}
		#endregion
	}
}
