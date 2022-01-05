using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Calculator712
{
	class GridMesh
	{
		readonly Grid grid;
		readonly List<List<Cell>> rows;
		readonly List<List<Cell>> columns;
		readonly CellsEnumerable cells;

		internal static GridMesh AssignTo(Grid grid)
		{
			return new GridMesh(grid);
		}
		internal GridMesh(Grid grid)
		{
			this.grid = grid;
			rows = new List<List<Cell>>();
			columns = new List<List<Cell>>();
			cells = new CellsEnumerable(rows, columns);
		}

		internal int RowsCount => rows.Count;
		internal int ColumnsCount => columns.Count;
		internal int CellsCount => rows.Count * columns.Count;
		internal CellsEnumerable Cells => cells;

		internal List<Cell> GetRow(int index)
		{
			return rows[index];
		}
		internal List<Cell> GetColumn(int index)
		{
			return columns[index];
		}
		internal Cell Pick(int row, int column)
		{
			return rows[row][column];
		}
		internal void AddRow()
		{
			var row = new List<Cell>(columns.Count);
			int currentRowIndex = rows.Count;
			var cells = CreateCells();
			row.AddRange(cells);
			rows.Add(row);
			var rowDef = new RowDefinition();
			grid.RowDefinitions.Add(rowDef);

			for (int i = 0; i < columns.Count; i++)
			{
				var column = columns[i];
				column.Add(cells[i]);
			}

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(columns.Count);
				for (int i = 0; i < columns.Count; i++)
				{
					var cell = new Cell(currentRowIndex, i);
					cell.ContentChanged += OnCellContentChanged;
					result.Add(cell);
				}
				return result;
			}
		}
		internal void AddColumn()
		{
			var column = new List<Cell>(rows.Count);
			int currentColumnIndex = columns.Count;
			var cells = CreateCells();
			column.AddRange(cells);
			columns.Add(column);
			var columnDef = new ColumnDefinition();
			this.grid.ColumnDefinitions.Add(columnDef);
			for (int i = 0; i < rows.Count; i++)
			{
				var row = rows[i];
				row.Add(cells[i]);
			}

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(rows.Count);
				for (int i = 0; i < rows.Count; i++)
				{
					var cell = new Cell(i, currentColumnIndex);
					cell.ContentChanged += OnCellContentChanged;
					result.Add(cell);
				}
				return result;
			}
		}

		internal void Slice(int rows, int columns)
		{
			for (int i = 0; i < rows; i++)
			{
				AddRow();
			}
			for (int i = 0; i < columns; i++)
			{
				AddColumn();
			}
		}
		void OnCellContentChanged(Cell cell, UIElement content)
		{
			if (content is null)
			{
				ClearCellContent(cell);
			}
			grid.Children.Add(content);
			Grid.SetRow(content, cell.Row);
			Grid.SetColumn(content, cell.Column);
		}
		void ClearCellContent(Cell cell)
		{
			var row = grid.RowDefinitions[3];
			var column = grid.ColumnDefinitions[3];
		}
		internal void SwapPositions(Cell a, Cell b)
		{
			int aOriginalRow = a.Row;
			int aOriginalColumn = a.Column;
			ChangeCellPosition(a, b.Row, b.Column);
			ChangeCellPosition(b, aOriginalRow, aOriginalColumn);
		}
		internal void ChangeCellPosition(Cell cell, int targetRow, int targetColumn) // TODO: проверка параметров
		{
			cell.Row = targetRow;
			cell.Column = targetColumn;
			Grid.SetRow(cell.Content, targetRow);
			Grid.SetColumn(cell.Content, targetColumn);
			RemoveCellFromOriginalPosition();
			AddCellToNewLocation();

			void RemoveCellFromOriginalPosition()
			{
				rows[cell.Row][cell.Column] = null;
				columns[cell.Column][cell.Row] = null;
			}
			void AddCellToNewLocation()
			{
				rows[targetRow][targetColumn] = cell;
				columns[targetColumn][targetRow] = cell;
			}
		}


		internal class Cell
		{
			internal Action<Cell, UIElement> ContentChanged = delegate { };

			internal Cell(int row, int column)
			{
				Row = row;
				Column = column;
			}

			internal bool Filled => Content != null;
			internal bool Blocked { get; set; }
			public int Column { get; internal set; }
			public int Row { get; internal set; }
			UIElement _content;
			internal UIElement Content
			{
				get => _content;
				set
				{
					_content = value;
					ContentChanged(this, value);
				}
			}

			internal void ClearContent()
			{
				Content = null;
			}
		}
		internal class CellsEnumerable : IEnumerable<Cell>
		{
			readonly List<List<Cell>> rows;
			readonly List<List<Cell>> columns;

			public CellsEnumerable(List<List<Cell>> rows, List<List<Cell>> columns)
			{
				this.rows = rows;
				this.columns = columns;
			}

			public IEnumerator<Cell> GetEnumerator()
			{
				return new CellEnumerator(this);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			class CellEnumerator : IEnumerator<Cell>
			{
				readonly CellsEnumerable cells;
				int currentRow;
				int currentColumn;

				public CellEnumerator(CellsEnumerable cells)
				{
					this.cells = cells;
					currentRow = 0;
					currentColumn = -1;
				}

				public Cell Current => cells.rows[currentRow][currentColumn];
				object IEnumerator.Current => Current;
				int RowsCount => cells.rows.Count;
				int ColumnsCount => cells.columns.Count;

				public void Dispose() { }
				public bool MoveNext()
				{
					if (currentColumn++ == ColumnsCount - 1)
					{
						currentRow++;
						currentColumn = 0;
					}
					return currentRow != RowsCount;
				}
				public void Reset()
				{
					currentRow = 0;
					currentColumn = -1;
				}
			}
		}
	}
}
