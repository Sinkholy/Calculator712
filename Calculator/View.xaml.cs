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
		GridMesh mainMesh;
		GridMesh controlMesh;
		GridMesh outputMesh;

		OperationButtonsPanel operationsPanel;
		NumericButtonsPanel numericPanel;
		Input input;
		HistoryBox history;
		UtilityPanel utilities;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View()
		{
			InitializeComponent();

			mainMesh = GridMesh.AssignTo(MainGrid);
			mainMesh.SetSize(2, 1);

			var controlGrid = new Grid();
			controlMesh = GridMesh.AssignTo(controlGrid);
			controlMesh.SetSize(1, 3);

			var outputGrid = new Grid();
			outputMesh = GridMesh.AssignTo(outputGrid);
			outputMesh.SetSize(1, 2);

			mainMesh.Pick(0, 0).Content = controlGrid;
			mainMesh.Pick(1, 0).Content = outputGrid;

			var numericPanelLayout = new DefaultNumpadLayout();
			numericPanel = new NumericButtonsPanel(numericPanelLayout);
			numericPanel.ButtonPressed += NumericButtonClickHandler;
			controlMesh.Pick(0, 0).Content = numericPanel;

			operationsPanel = new OperationButtonsPanel();
			operationsPanel.ButtonPressed += OperationButtonClickHandler;
			controlMesh.Pick(0, 1).Content = operationsPanel;

			utilities = new UtilityPanel();
			utilities.BackspaceButtonClicked += BackspaceButtonClickHandler;
			utilities.ClearButtonClicked += ClearButtonClickHandler;
			utilities.CalculateButtonClicked += CalculationButtonClickHandler;
			controlMesh.Pick(0, 2).Content = utilities;

			input = new Input();
			outputMesh.Pick(0, 1).Content = input.View;

			history = new HistoryBox();
			outputMesh.Pick(0, 0).Content = history.View;
		}

		public void AddOperation(ICalculatorOperation operation)
		{
			operationsPanel.AddOperation(operation.Symbol);
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

		class Input // TODO: читай HistoryBox, здесь то же самое.
		{
			ComputationData computation;
			List<int> input;
			int firstOperandNumbersCount;
			InputView view;

			internal Input()
			{
				computation = new ComputationData();
				input = new List<int>();
				view = new InputView();
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
			internal UIElement View => view.View;

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

			class InputView
			{
				const string SpacerSymbol = " "; // TODO: спейсеры и их логика должны быть здесь?

				readonly TextBlock text;

				public InputView()
				{
					text = new TextBlock();
				}

				internal UIElement View => text;

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
		class HistoryBox // TODO: нужно подумать над тем, как реализовать в этом классе UIElement
						 // чтобы не делать матрёшку из вызовов View.
		{
			readonly Archive archive;
			readonly HistoryView view;

			internal HistoryBox()
			{
				archive = new Archive();
				view = new HistoryView();
			}

			internal UIElement View => view.View; // TODO: ужасная матрёшка

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

			class HistoryView : UIElement
			{
				readonly ListBox list;

				internal HistoryView()
				{
					list = new ListBox();
				}

				internal UIElement View => list;

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
		class UtilityPanel : Grid
		{
			const string CalculationButtonSymbol = "=";
			const string BackspaceButtonSymbol = "<-";
			const string ClearButtonSymbol = "Clear";

			readonly GridMesh mesh;

			internal UtilityPanel()
			{
				mesh = GridMesh.AssignTo(this);
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

		internal abstract class MeshLayout
		{
			public abstract void ApplyTo(GridMesh target);
		}
		internal abstract class NumericPanelLayout : MeshLayout
		{
			public abstract void ApplyTo(GridMesh target, IButtonsProvider buttonsProvider);

			public interface IButtonsProvider
			{
				UIElement GetButtonWithNumber(int num);
			}
		}
		class DefaultNumpadLayout : NumericPanelLayout
		{
			const int ButtonsCount = 10;

			public override void ApplyTo(GridMesh target, IButtonsProvider buttonsProvider)
			{
				UIElement[] buttons = CreateButtons(buttonsProvider);
				target.SetSize(4, 3);
				ApplyLayout(target, buttons);
			}
			UIElement[] CreateButtons(IButtonsProvider buttonsProvider)
			{
				UIElement[] result = new UIElement[ButtonsCount];
				for (int i = 0; i < ButtonsCount; i++)
				{
					result[i] = buttonsProvider.GetButtonWithNumber(i);
				}
				return result;
			}
			void ApplyLayout(GridMesh target, UIElement[] buttons)
			{
				target.Pick(3, 1).Content = buttons[0];
				target.Pick(0, 0).Content = buttons[1];
				target.Pick(0, 1).Content = buttons[2];
				target.Pick(0, 2).Content = buttons[3];
				target.Pick(1, 0).Content = buttons[4];
				target.Pick(1, 1).Content = buttons[5];
				target.Pick(1, 2).Content = buttons[6];
				target.Pick(2, 0).Content = buttons[7];
				target.Pick(2, 1).Content = buttons[8];
				target.Pick(2, 2).Content = buttons[9];
			}

			public override void ApplyTo(GridMesh target)
			{
				throw new NotImplementedException();
			}
		}
		class NumericButtonsPanel : Grid
		{
			GridMesh mesh;

			internal Action<int> ButtonPressed = delegate { };

			internal NumericButtonsPanel(NumericPanelLayout layout)
			{
				mesh = GridMesh.AssignTo(this);
				var provider = new NumericButtonProvider(ButtonClickHandler);
				layout.ApplyTo(mesh, provider);
			}

			void ButtonClickHandler(object sender, RoutedEventArgs args)
			{
				var button = sender as NumericButton;
				ButtonPressed(button.Value);
			}

			class NumericButtonProvider : NumericPanelLayout.IButtonsProvider
			{
				Action<object, RoutedEventArgs> callback;

				public NumericButtonProvider(Action<object, RoutedEventArgs> buttonClickCallback)
				{
					callback = buttonClickCallback;
				}

				public UIElement GetButtonWithNumber(int num)
				{
					var button = NumericButton.WithNumber(num);
					button.Click += (o, e) => callback(o, e);
					return button;
				}
			}
			class NumericButton : Button
			{
				internal static NumericButton WithNumber(int num)
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
		class OperationButtonsPanel : Grid
		{
			readonly GridMesh mesh;

			internal Action<string> ButtonPressed = delegate { };

			public OperationButtonsPanel()
			{
				mesh = GridMesh.AssignTo(this);
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
	}
}