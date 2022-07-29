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

		IPanel[] panels => new IPanel[5] { numericPanel, utilities, history, input, operationsPanel };
		OperationButtonsPanel operationsPanel;
		NumericButtonsPanel numericPanel;
		InputPanel input;
		HistoryPanel history;
		UtilityPanel utilities;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View(XDocument layout, string[] operationsSymbols)
		{
			InitializeComponent();

			mainMesh = GridMesh.AssignTo(MainGrid);
			CreateNumericPanel();
			CreateOperationsPanel();
			CreateUtilitiesPanel();
			input = new InputPanel();
			history = new HistoryPanel();

			Layout = layout;

			void CreateNumericPanel()
			{
				numericPanel = new NumericButtonsPanel();
				numericPanel.ButtonPressed += NumericButtonClickHandler;
			}
			void CreateOperationsPanel()
			{
				operationsPanel = new OperationButtonsPanel(operationsSymbols);
				operationsPanel.ButtonPressed += OperationButtonClickHandler;
			}
			void CreateUtilitiesPanel()
			{
				utilities = new UtilityPanel();
				utilities.BackspaceButtonClicked += BackspaceButtonClickHandler;
				utilities.ClearButtonClicked += ClearButtonClickHandler;
				utilities.CalculateButtonClicked += CalculationButtonClickHandler;
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
			SetupPanels();
			AllignPanels();

			void ApplyMainPanelLayout()
			{
				var columnsSize = int.Parse(Layout.Root.Element("size").Element("columns").Value);
				var rowsSize = int.Parse(Layout.Root.Element("size").Element("rows").Value);
				mainMesh.SetSize(rowsSize, columnsSize);
			}
			void SetupPanels()
			{
				var panelsNode = Layout.Root.Element("panels");
				foreach (var panelNode in panelsNode.Elements("panel"))
				{
					var panelName = panelNode.Attribute("name").Value;
					var panel = panels.Single(x => x.Name == panelName); // TODO: потенциальное место появления ошибок

					panel.Position = new IPanel.PanelPosition()
					{
						Column = int.Parse(panelNode.Element("position").Element("column").Value),
						Row = int.Parse(panelNode.Element("position").Element("row").Value)
					};

					if (panel is ILayoutablePanel layoutablePanel)
					{
						layoutablePanel.ApplyLayout(panelNode);
					}
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
		void NumericButtonClickHandler(int value)
		{
			if (input.ContainsResult)
			{
				history.Add(input.Computation);
				input.Clear();
			}
			input.AddToCurrentOperand(value);
		}
		void OperationButtonClickHandler(string operation)
		{
			if (!input.OperationAdded)
			{
				input.SetOperation(operation);
			}
		}
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
		class UtilityPanel : IPanel, ILayoutablePanel
		{
			#region IPanel impl
			public UIElement UIElement => this;
			public string Name => "utilities";
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			#endregion
			#region ILayoutable impl
			public void ApplyLayout(XElement layoutSerialized)
			{
				SetMeshSize();
				SetButtons();

				void SetMeshSize()
				{
					var columnsSize = int.Parse(layoutSerialized.Element("size").Element("columns").Value);
					var rowsSize = int.Parse(layoutSerialized.Element("size").Element("rows").Value);
					mesh.SetSize(rowsSize, columnsSize);
				}
				void SetButtons()
				{
					foreach (var buttonNode in layoutSerialized.Element("buttons").Elements("button"))
					{
						var buttonSymbol = buttonNode.Attribute("symbol").Value; // TODO: нужна ли здесь проверка на наличие необходимого обработчика?
						var buttonRow = int.Parse(buttonNode.Element("position").Element("row").Value);
						var buttonColumn = int.Parse(buttonNode.Element("position").Element("column").Value);

						var button = new Button() { Content = buttonSymbol };
						button.Click += OnButtonClicked;

						mesh.Pick(buttonRow, buttonColumn).Content = button;
					}
				}
			}

			public XElement ExtractLayout()
			{
				var sizeElem = new XElement("size",
							new XElement("columns", mesh.ColumnsCount),
									new XElement("rows", mesh.RowsCount));
				var positionElem = new XElement("position",
										new XElement("column", Position.Column),
										new XElement("row", Position.Row));

				var buttonsElem = new XElement("buttons");
				foreach (var cell in mesh.Cells)
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
			#endregion
			const string CalculationButtonSymbol = "=";
			const string BackspaceButtonSymbol = "<-";
			const string ClearButtonSymbol = "Clear";

			readonly GridMesh mesh;

			public static implicit operator UIElement(UtilityPanel panel)
			{
				return panel.mesh;
			}

			internal UtilityPanel()
			{
				mesh = new GridMesh();
			}

			internal Action BackspaceButtonClicked = delegate { };
			internal Action ClearButtonClicked = delegate { };
			internal Action CalculateButtonClicked = delegate { };

			void OnButtonClicked(object obj, RoutedEventArgs args)
			{
				var button = obj as Button;
				switch (button.Content)
				{
					case CalculationButtonSymbol:
						CalculateButtonClicked();
						break;
					case ClearButtonSymbol:
						ClearButtonClicked();
						break;
					case BackspaceButtonSymbol:
						BackspaceButtonClicked();
						break;
					default:
						throw new ArgumentException(); // TODO: исключение
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
		class NumericButtonsPanel : IPanel, ILayoutablePanel
		{
			#region IPanel impl
			public string Name => "numericButtons";
			public UIElement UIElement => this;
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			#endregion
			#region ILayoutable impl
			public void ApplyLayout(XElement layoutSerialized)
			{
				SetMeshSize();
				SetButtons();

				void SetMeshSize()
				{
					var columnsSize = int.Parse(layoutSerialized.Element("size").Element("columns").Value);
					var rowsSize = int.Parse(layoutSerialized.Element("size").Element("rows").Value);
					Mesh.SetSize(rowsSize, columnsSize);
				}
				void SetButtons()
				{
					foreach (var buttonNode in layoutSerialized.Element("buttons").Elements("button"))
					{
						var buttonNumber = int.Parse(buttonNode.Attribute("symbol").Value);
						var buttonRow = int.Parse(buttonNode.Element("position").Element("row").Value);
						var buttonColumn = int.Parse(buttonNode.Element("position").Element("column").Value);

						var button = NumericButton.CreateWithNumber(buttonNumber);
						button.Click += ButtonClickHandler;

						Mesh.Pick(buttonRow, buttonColumn).Content = button;
					}
				}
			}

			public XElement ExtractLayout()
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
			#endregion
			public static implicit operator UIElement(NumericButtonsPanel panel)
			{
				return panel.Mesh;
			}

			internal Action<int> ButtonPressed = delegate { };

			internal GridMesh Mesh { get; private set; }

			internal NumericButtonsPanel()
			{
				Mesh = new GridMesh();
			}

			void ButtonClickHandler(object sender, RoutedEventArgs args)
			{
				var button = sender as NumericButton;
				ButtonPressed(button.Value);
			}

			class NumericButton : Button
			{
				internal static NumericButton CreateWithNumber(int num)
				{
					return new NumericButton()
					{
						Value = num
					};
				}

				int value;
				internal int Value
				{
					get => value;
					set
					{
						this.value = value;
						Content = value;
					}
				}
			}
		}
		class OperationButtonsPanel : IPanel, ILayoutablePanel
		{
			#region IPanel impl
			public UIElement UIElement => this;
			public string Name => "operationButtons";
			public bool PositionLocked { get; set; }
			public IPanel.PanelPosition Position { get; set; }
			#endregion
			#region ILayoutable impl
			public void ApplyLayout(XElement layoutSerialized)
			{
				SetMeshSize();
				SetButtons();

				void SetMeshSize()
				{
					var columnsSize = int.Parse(layoutSerialized.Element("size").Element("columns").Value);
					var rowsSize = int.Parse(layoutSerialized.Element("size").Element("rows").Value);
					mesh.SetSize(rowsSize, columnsSize);
				}
				void SetButtons()
				{
					foreach (var buttonNode in layoutSerialized.Element("buttons").Elements("button"))
					{
						var buttonSymbol = buttonNode.Attribute("symbol").Value;
						var buttonRow = int.Parse(buttonNode.Element("position").Element("row").Value);
						var buttonColumn = int.Parse(buttonNode.Element("position").Element("column").Value);

						var button = new Button() { Content = buttonSymbol };
						button.Click += ButtonClickHandler;

						mesh.Pick(buttonRow, buttonColumn).Content = button;
					}
				}
			}

			public XElement ExtractLayout()
			{
				throw new NotImplementedException();
			}
			#endregion
			readonly GridMesh mesh;
			string[] operationsSymbols;

			public static implicit operator UIElement(OperationButtonsPanel panel)
			{
				return panel.mesh;
			}

			internal Action<string> ButtonPressed = delegate { };

			public OperationButtonsPanel(string[] operations)
			{
				mesh = new GridMesh();
				operationsSymbols = operations;
			}

			internal void AddOperation(string symbol)
			{
				if (!mesh.Cells.ContainsEmptyCells)
				{
					ExtendGrid();
				}
				var emptyCell = mesh.Cells.Empty.First();
				emptyCell.Content = CreateButton();

				void ExtendGrid()
				{
					if (mesh.ColumnsCount < mesh.RowsCount)
					{
						mesh.AddColumn();
					}
					else
					{
						mesh.AddRow();
					}
				}
				Button CreateButton()
				{
					var button = new Button() { Content = symbol };
					button.Click += ButtonClickHandler;
					return button;
				}
			}

			void ButtonClickHandler(object sender, RoutedEventArgs args)
			{
				var button = sender as Button;
				var operation = button.Content as string;
				ButtonPressed(operation); // TODO: possible NRE?
			}
		}
		#endregion
	}
}