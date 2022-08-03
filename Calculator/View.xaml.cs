using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	/// <summary>
	/// Interaction logic for Window.xaml
	/// </summary>
	public partial class View : Window
	{
		readonly GridMesh mainMesh;

		string[] operationsSymbols;
		string[] numericSymbols = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"};
		string[] utilitiesSymbols = { "=", "<-" , "Clear" };
		List<IPanel> panels;
		InputPanel input;
		HistoryPanel history;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View(XDocument layout, string[] operationsSymbols)
		{
			InitializeComponent();

			this.operationsSymbols = operationsSymbols;
			panels = new List<IPanel>(10);
			mainMesh = GridMesh.AssignTo(MainGrid);
			CreatePanels();
			Layout = layout;

			void CreatePanels()
			{
				input = new InputPanel();
				history = new HistoryPanel();
			}
		}
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
			ApplyMainPanelLayout();
			var panelsNode = Layout.Root.Element("panels");
			SetupDefaultPanels();
			SetupLayoutablePanels();
			AllignPanels();

			void ApplyMainPanelLayout()
			{
				var columnsSize = int.Parse(Layout.Root.Element("size").Element("columns").Value);
				var rowsSize = int.Parse(Layout.Root.Element("size").Element("rows").Value);
				mainMesh.SetSize(rowsSize, columnsSize);
			}
			void SetupDefaultPanels()
			{
				var historyPanelNode = panelsNode.Element("defaultPanels").Elements("panel").Single(x => x.Attribute("name").Value == "history");
				history.Position = new IPanel.PanelPosition()
				{
					Column = int.Parse(historyPanelNode.Element("position").Element("column").Value),
					Row = int.Parse(historyPanelNode.Element("position").Element("row").Value)
				};

				var inputPanelNode = panelsNode.Element("defaultPanels").Elements("panel").Single(x => x.Attribute("name").Value == "input");
				input.Position = new IPanel.PanelPosition()
				{
					Column = int.Parse(inputPanelNode.Element("position").Element("column").Value),
					Row = int.Parse(inputPanelNode.Element("position").Element("row").Value)
				};

				mainMesh.Pick(input.Position.Row, input.Position.Column).Content = input.UIElement;
				mainMesh.Pick(history.Position.Row, history.Position.Column).Content = history.UIElement;

				foreach (var panelNode in panelsNode.Elements("defaultPanels"))
				{
					// TODO: undone
				}
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
			void AllignPanels()
			{
				foreach (var panel in panels)
				{
					mainMesh.Pick(panel.Position.Row, panel.Position.Column).Content = panel.UIElement;
				}
			}
		}
		#endregion
		#region Panels buttons handlers
		void PanelButtonCommandRouter(IPanel panel, Button button)
		{
			var symbol = button.Content.ToString();
			if (numericSymbols.Contains(symbol))
			{
				NumericButtonClickHandler(symbol);
			}
			else if (operationsSymbols.Contains(symbol))
			{
				OperationButtonClickHandler(symbol);
			}
			else if (utilitiesSymbols.Contains(symbol))
			{
				UtilitiesButtonClickHandler(symbol);
			}
			else
			{
				throw new ArgumentException(); // TODO: исключение
			}
		}
		void NumericButtonClickHandler(string symbol)
		{
			var number = int.Parse(symbol);
			if (input.ContainsResult)
			{
				history.Add(input.Computation);
				input.Clear();
			}
			input.AddToCurrentOperand(number);
		}
		void OperationButtonClickHandler(string operation)
		{
			if (!input.OperationAdded)
			{
				input.SetOperation(operation);
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
				if (input.ComputationReadyToProcess)
				{
					var current = input.Computation;
					var calculationData = new CalculationData(current.Symbol, current.LeftOperand, current.RightOperand);
					CalculationRequested(calculationData);
				}
			}
			void BackspaceButtonClickHandler()
			{
				input.Clear();
			}
			void ClearButtonClickHandler()
			{
				history.Clear();
			}
		}

		#endregion
		public void SetResult(int value)
		{
			input.SetResult(value);
		}

		public class CalculationData
		{
			public CalculationData(string operationSymbol, int leftOperand, int rightOperand)
			{
				OperationSymbol = operationSymbol;
				LeftOperand = leftOperand;
				RightOperand = rightOperand;
			}

			public string OperationSymbol { get; }
			public int LeftOperand { get; }
			public int RightOperand { get; }
		}

		#region Panels
		interface IPanel
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

			ComputationData computation;
			List<int> input;
			int firstOperandNumbersCount;
			View view;

			public static implicit operator UIElement(InputPanel panel)
			{
				return panel.view;
			}

			internal InputPanel()
			{
				computation = new ComputationData();
				input = new List<int>();
				view = new View();
			}

			internal bool ContainsLeftOperand => input.Count > 0;
			internal bool ContainsRightOperand => firstOperandNumbersCount > 0;
			internal bool OperationAdded => !string.IsNullOrWhiteSpace(computation.Symbol);
			internal bool ContainsResult => computation.Result != 0; // UNDONE: затычка
			internal bool ComputationReadyToProcess => ContainsLeftOperand && OperationAdded && ContainsRightOperand;
			internal ComputationData Computation
			{
				get
				{
					CalculateOperands();
					return computation;
				}
			}

			internal void AddToCurrentOperand(int value)
			{
				input.Add(value);
				view.Append(value.ToString());
			}
			internal void SetOperation(string symbol)
			{
				computation.Symbol = symbol;
				firstOperandNumbersCount = input.Count;
				view.Append(symbol);
			}
			internal void SetResult(int value)
			{
				computation.Result = value;
				view.Set(computation.ToString());
			}
			internal void Clear()
			{
				input.Clear();
				firstOperandNumbersCount = 0;
				view.Clear();
				computation = new ComputationData();
			}
			void CalculateOperands()
			{
				computation.LeftOperand = CalculateLeftOperand();
				computation.RightOperand = CalculateRightOperand();

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

			internal IReadOnlyCollection<ComputationData> GetArchive()
			{
				return archive.GetFullArchive();
			}
			internal void Add(ComputationData computation)
			{
				archive.Add(computation);
				view.Add(computation.ToString());
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
				readonly List<ComputationData> computations;

				internal Archive()
				{
					computations = new List<ComputationData>();
				}

				internal void Add(ComputationData computation)
				{
					computations.Add(computation);
				}
				internal void Clear()
				{
					computations.Clear();
				}
				internal ComputationData this[int index]
				{
					get
					{
						return computations[index];
					}
				}
				internal IReadOnlyCollection<ComputationData> GetFullArchive()
				{
					return computations;
				}
			}
		}
		class ComputationData
		{
			const string EqualSign = "=";
			const string Spacer = " ";

			internal int LeftOperand { get; set; }
			internal int RightOperand { get; set; }
			internal string Symbol { get; set; }
			internal int Result { get; set; }

			public override string ToString()
			{
				return $"{LeftOperand}{Spacer}{Symbol}{Spacer}{RightOperand}{Spacer}{EqualSign}{Spacer}{Result}";
			}
		}
		#endregion
	}
}