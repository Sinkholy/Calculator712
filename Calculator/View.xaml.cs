using System;
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

		public Action<CalculationData> CalculationRequested = delegate { };

		public View()
		{
			InitializeComponent();
			numericPanel = new NumericButtonsPanel();
			numericPanel.ButtonPressed += NumericButtonClickHandler;

			ButtonsGrid.Children.Add(numericPanel);
			Grid.SetColumn(numericPanel, 0);
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
			var expression = ResultTextBox.Text;
			string[] parsed = expression.Split(" ");
			var leftOperand = parsed[0];
			var operationSymbol = parsed[1];
			var rightOperand = parsed[2];
			var calculationData = new CalculationData(operationSymbol, leftOperand, rightOperand);
			CalculationRequested(calculationData);
		}
		void BackspaceButtonClickHandler(object sender, RoutedEventArgs args)
		{
			ResultTextBox.Text = "";
		}
		void NumericButtonClickHandler(int value)
		{
			AddStringToResultPanel(value.ToString());
		}
		void OperationButtonClickHandler(object sender, RoutedEventArgs args)
		{
			var button = sender as Button;
			string operation = button.Content as string;
			AddStringToResultPanel(" ");
			AddStringToResultPanel(operation);
			AddStringToResultPanel(" ");
		}
		void AddStringToResultPanel(string value)
		{
			ResultTextBox.Text += value;
		}
		public void SetResult(string value)
		{
			AddStringToResultPanel(" ");
			AddStringToResultPanel("=");
			AddStringToResultPanel(" ");
			AddStringToResultPanel(value);
		}

		public class CalculationData
		{
			public CalculationData(string operationSymbol, string leftOperand, string rightOperand)
			{
				OperationSymbol = operationSymbol;
				LeftOperand = leftOperand;
				RightOperand = rightOperand;
			}

			public string OperationSymbol { get; }
			public string LeftOperand { get; }
			public string RightOperand { get; }
		}

		class NumericButtonsPanel : Grid
		{
			GridMesh mesh;

			internal Action<int> ButtonPressed = delegate { };

			internal NumericButtonsPanel()
			{
				mesh = GridMesh.AssignTo(this);
				ApplyLayout();
			}
			void ApplyLayout()
			{
				mesh.Slice(4,3);

				mesh.Pick(3, 1).Content = NumericButton.WithNumber(0);
				mesh.Pick(0, 0).Content = NumericButton.WithNumber(1);
				mesh.Pick(0, 1).Content = NumericButton.WithNumber(2);
				mesh.Pick(0, 2).Content = NumericButton.WithNumber(3);
				mesh.Pick(1, 0).Content = NumericButton.WithNumber(4);
				mesh.Pick(1, 1).Content = NumericButton.WithNumber(5);
				mesh.Pick(1, 2).Content = NumericButton.WithNumber(6);
				mesh.Pick(2, 0).Content = NumericButton.WithNumber(7);
				mesh.Pick(2, 1).Content = NumericButton.WithNumber(8);
				mesh.Pick(2, 2).Content = NumericButton.WithNumber(9);

				foreach (var cell in mesh.Cells)
				{
					if(cell.Content is Button button)
					{
						button.Click += ButtonClickHandler;
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
