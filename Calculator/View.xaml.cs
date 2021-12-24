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
		const string CalculationButtonSymbol = "=";
		const string BackspaceButtonSymbol = "<-";

		NumericButtonsPanel numericPanel;
		ResultPanel resultPanel;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View()
		{
			InitializeComponent();
			var numericPanelLayout = new DefaultNumpadLayout();
			numericPanel = new NumericButtonsPanel(numericPanelLayout);
			numericPanel.ButtonPressed += NumericButtonClickHandler;
			
			ButtonsGrid.Children.Add(numericPanel);
			Grid.SetColumn(numericPanel, 0);

			resultPanel = new ResultPanel();
			ResultGrid.Children.Add(resultPanel);
		}

		public void AddOperation(ICalculatorOperation operation)
		{
			var operationButton = new Button()
			{
				Content = operation.Symbol
			};
			operationButton.Click += OperationButtonClickHandler;
			ButtonsGrid.Children.Add(operationButton);
			Grid.SetColumn(operationButton, 1);
		}

		void CalculationButtonClickHandler(object sender, RoutedEventArgs args)
		 {
			var current = resultPanel.GetCurrent();
			var calculationData = new CalculationData(current.symbol, current.a, current.b);
			CalculationRequested(calculationData);
		}
		void BackspaceButtonClickHandler(object sender, RoutedEventArgs args)
		{
			//ResultTextBox.Text = "";
		}
		void NumericButtonClickHandler(int value)
		{
			resultPanel.AppendInput(value);
		}
		void OperationButtonClickHandler(object sender, RoutedEventArgs args)
		{
			var button = sender as Button;
			string operation = button.Content as string;
			resultPanel.AppendSymbol(operation);
		}
		public void SetResult(int value)
		{
			resultPanel.SetResult(value);
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

		class ResultPanel : Grid
		{
			GridMesh mesh;
			Input input;
			HistoryBox history;

			public ResultPanel()
			{
				mesh = GridMesh.AssignTo(this);
				input = new Input();
				history = new HistoryBox();
				ApplyLayout();
			}
			void ApplyLayout()
			{
				mesh.Slice(2, 1);

				mesh.Pick(1, 0).Content = input.View;
				mesh.Pick(0, 0).Content = history.View;
			}

			internal (int a, int b, string symbol) GetCurrent()
			{
				return new(input.Computation.LeftOperand,
						input.Computation.RightOperand,
						input.Computation.Symbol);
			}
			internal void AppendInput(int value)
			{
				input.AddToCurrentOperand(value);
			}
			internal void AppendSymbol(string symbol)
			{
				input.SetOperation(symbol);
			}
			internal void SetResult(int value)
			{
				input.SetResult(value);
			}
			internal void PushResultToHistory()
			{
				history.Add(input.Computation);  
			}
			internal void ClearHistory()
			{
				history.Clear();
			}
			internal void ClearResult()
			{
				input.Clear();
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
				target.Slice(4, 3);
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
	}
}