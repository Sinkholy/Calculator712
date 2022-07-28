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

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	/// <summary>
	/// Interaction logic for Window.xaml
	/// </summary>
	public partial class View : Window
	{
		readonly GridMesh mainMesh;

		OperationButtonsPanel operationsPanel;
		NumericButtonsPanel numericPanel;
		InputPanel input;
		HistoryPanel history;
		UtilityPanel utilities;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View(INumericButtonPanelLayout numericButtonsPanelLayout, string[] operationsSymbols)
		{
			InitializeComponent();

			mainMesh = CreateMainMesh();
			var controlMesh = CreateContolsPanels();
			var outputMesh = CreateOutputPanels();
			AlignPanels();

			GridMesh CreateMainMesh()
			{
				var mesh = GridMesh.AssignTo(MainGrid);
				mesh.SetSize(2, 1);
				return mesh;
			}
			GridMesh CreateContolsPanels()
			{
				var mesh = GridMesh.CreateWithSize(1, 3);
				numericPanel = CreateNumericPanel();
				operationsPanel = CreateperationsPanel();
				utilities = CreateUtilitiesPanel();				
				AlignPanels();
				return mesh; 

				NumericButtonsPanel CreateNumericPanel()
				{
					var numericPanel = new NumericButtonsPanel(numericButtonsPanelLayout);
					numericPanel.ButtonPressed += NumericButtonClickHandler;
					return numericPanel;
				}
				OperationButtonsPanel CreateperationsPanel()
				{
					var operationsPanel = new OperationButtonsPanel(operationsSymbols);
					operationsPanel.ButtonPressed += OperationButtonClickHandler;
					return operationsPanel;					
				}
				UtilityPanel CreateUtilitiesPanel()
				{
					var utilitiesPanel = new UtilityPanel();
					utilitiesPanel.BackspaceButtonClicked += BackspaceButtonClickHandler;
					utilitiesPanel.ClearButtonClicked += ClearButtonClickHandler;
					utilitiesPanel.CalculateButtonClicked += CalculationButtonClickHandler;
					return utilitiesPanel;
				}
				void AlignPanels()
				{	
					mesh.Pick(0, 0).Content = numericPanel;
					mesh.Pick(0, 1).Content = operationsPanel;
					mesh.Pick(0, 2).Content = utilities;
				}
			}
			GridMesh CreateOutputPanels()
			{
				var mesh = GridMesh.CreateWithSize(1, 2);
				input = CreateInputPanel();
				history = CreateHistoryPanel();
				AlignPanels();
				return mesh;

				InputPanel CreateInputPanel()
				{
					return new InputPanel();
				}
				HistoryPanel CreateHistoryPanel()
				{
					return new HistoryPanel();
				}
				void AlignPanels()
				{
					mesh.Pick(0, 1).Content = input;
					mesh.Pick(0, 0).Content = history;
				}
			}
			void AlignPanels()
			{
				mainMesh.Pick(0, 0).Content = controlMesh;
				mainMesh.Pick(1, 0).Content = outputMesh;
			}
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
		class InputPanel
		{
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
		class HistoryPanel
		{
			readonly Archive archive;
			readonly View view;

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
		class UtilityPanel
		{
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
				mesh.SetSize(1, 1);
				Setup();
			}

			internal Action BackspaceButtonClicked = delegate { };
			internal Action ClearButtonClicked = delegate { };
			internal Action CalculateButtonClicked = delegate { };

			void Setup()
			{
				SetBackspaceButton();
				SetCalculateButton();
				SetClearButton();

				void SetBackspaceButton()
				{
					var button = new Button()
					{
						Content = BackspaceButtonSymbol
					};
					button.Click += (_, _) => BackspaceButtonClicked();

					var cell = GetNextEmptyCell();
					cell.Content = button;
				}
				void SetCalculateButton()
				{
					var button = new Button()
					{
						Content = CalculationButtonSymbol
					};
					button.Click += (_, _) => CalculateButtonClicked();

					var cell = GetNextEmptyCell();
					cell.Content = button;
				}
				void SetClearButton()
				{
					var button = new Button()
					{
						Content = ClearButtonSymbol
					};
					button.Click += (_, _) => ClearButtonClicked();

					var cell = GetNextEmptyCell();
					cell.Content = button;
				}
				GridMesh.Cell GetNextEmptyCell()
				{
					if (!mesh.Cells.ContainsEmptyCells)
					{
						if(mesh.ColumnsCount > mesh.RowsCount)
						{
							mesh.AddRow();
						}
						else
						{
							mesh.AddColumn();
						}
					}
					return mesh.Cells.Empty.First();
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
		class NumericButtonsPanel
		{
			const int ButtonsCount = 10;

			GridMesh mesh;

			public static implicit operator UIElement(NumericButtonsPanel panel)
			{
				return panel.mesh;
			}

			internal Action<int> ButtonPressed = delegate { };

			internal NumericButtonsPanel(INumericButtonPanelLayout layout)
			{
				mesh = GridMesh.CreateWithSize(layout.PanelRowsCount, layout.PanelColumnsCount);
				var buttons = CreateButtons();
				ApplyLayout();

				NumericButton[] CreateButtons()
				{
					var result = new NumericButton[ButtonsCount];
					for (int i = 0; i < ButtonsCount; i++)
					{
						result[i] = NumericButton.CreateWithNumber(i);
						result[i].Click += ButtonClickHandler;
					}
					return result;
				}
				void ApplyLayout()
				{
					foreach (var button in buttons)
					{
						var buttonRow = layout.GetButtonRow(button.Value);
						var buttonColumn = layout.GetButtonColumn(button.Value);
						mesh.Pick(buttonRow, buttonColumn).Content = button;
					}
				}
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
		class OperationButtonsPanel
		{
			readonly GridMesh mesh;

			public static implicit operator UIElement(OperationButtonsPanel panel)
			{
				return panel.mesh;
			}

			internal Action<string> ButtonPressed = delegate { };

			public OperationButtonsPanel(string[] operations)
			{
				mesh = new GridMesh();
				foreach (var operation in operations)
				{
					AddOperation(operation);
				}
				ApplyLayout();
			}
			void ApplyLayout()
			{
				mesh.SetSize(1, 1);
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
		#region Layouts
		public interface INumericButtonPanelLayout
		{
			public int PanelColumnsCount { get; }
			public int PanelRowsCount { get; }

			public int GetButtonColumn(int buttonNumber);
			public int GetButtonRow(int buttonNumber);
		}
		#endregion
	}
}