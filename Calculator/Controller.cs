using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	public class Controller
	{
		public View View { get; }
		ICalculatorOperation[] Operations { get; }

		public Controller(ICalculatorOperation[] operations)
		{
			Operations = operations;
			View = new View();
		}

		public void Start()
		{
			foreach (var operation in Operations)
			{
				View.AddOperation(operation);
			}
			View.CalculationRequested += OnCalculationRequested;
		}
		void OnCalculationRequested(CalculationData data)
		{
			var targetOperation = GetOperationBySymbol(data.OperationSymbol);
			var result = targetOperation.Calculate(data.LeftOperand, data.RightOperand);
			View.SetResult(result);
		}
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
	}
}
