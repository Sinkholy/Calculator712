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

		public Action<CalculationData> CalculationRequested = delegate { };

		public View()
		{
			InitializeComponent();
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
		void NumericButtonClickHandler(object sender, RoutedEventArgs args)
		{
			var button = sender as Button;
			string number = button.Content as string;
			AddStringToResultPanel(number);
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
	}
}
