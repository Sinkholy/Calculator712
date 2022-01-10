using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Calculator712
{
	class GridMesh
	{
		readonly Grid grid;
		readonly List<List<Cell>> rows;
		readonly CellsEnumerable cells;

		internal static GridMesh AssignTo(Grid grid)
		{
			return new GridMesh(grid);
		}
		internal GridMesh(Grid grid)
		{
			this.grid = grid;
			rows = new List<List<Cell>>();
			cells = new CellsEnumerable(rows);
		}

		internal int RowsCount => rows.Count;
		internal int ColumnsCount => rows.Count == 0 ? 0 : rows[0].Count;
		internal int CellsCount => rows.Count * rows[0].Count;
		internal CellsEnumerable Cells => cells;

		internal IEnumerable<Cell> GetRow(int index)
		{
			return rows[index];
		}
		internal IEnumerable<Cell> GetColumn(int index) 
			=> rows.Select(row => row.ElementAt(index));
		internal Cell Pick(int row, int column)
		{
			return rows[row][column];
		}
		internal void AddRow()
		{
			var row = new List<Cell>(ColumnsCount);
			var cells = CreateCells();
			row.AddRange(cells);
			rows.Add(row);
			var rowDef = new RowDefinition();
			grid.RowDefinitions.Add(rowDef);

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(ColumnsCount);
				for (int i = 0; i < ColumnsCount; i++)
				{
					var cell = new Cell(rows.Count, i);
					cell.ContentChanged += OnCellContentChanged;
					cell.ContentCleared += OnCellContentCleared;
					cell.VisibilityChanged += OnCellVisibilityChanged;
					result.Add(cell);
				}
				return result;
			}
		}
		internal void AddColumn()
		{
			var col = new ColumnDefinition();
			grid.ColumnDefinitions.Add(col);
			var cells = CreateCells();
			for (int i = 0; i < RowsCount; i++)
			{
				var row = rows[i];
				var cell = cells[i];
				row.Add(cell);
			}

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(RowsCount);
				for (int i = 0; i < RowsCount; i++)
				{
					var cell = new Cell(i, ColumnsCount);
					cell.ContentChanged += OnCellContentChanged;
					cell.ContentCleared += OnCellContentCleared;
					cell.VisibilityChanged += OnCellVisibilityChanged;
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
		void OnCellContentChanged(Cell.ContentChangedArgs args)
		{
			grid.Children.Remove(args.PreviousContent);
			grid.Children.Add(args.NewContent);
			Grid.SetRow(args.NewContent, args.CellRow);
			Grid.SetColumn(args.NewContent, args.CellColumn);
		}
		void OnCellContentCleared(Cell.ContentChangedArgs args)
		{
			grid.Children.Remove(args.PreviousContent);
		}
		void OnCellVisibilityChanged(Cell.VisibilityChangedArgs args)
		{
			var cell = Pick(args.CellRow, args.CellColumn);
			cell.Content.Visibility = args.New;
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
			}
			void AddCellToNewLocation()
			{
				rows[targetRow][targetColumn] = cell;
			}
		}

		internal class Cell
		{
			internal Action<ContentChangedArgs> ContentChanged = delegate { };
			internal Action<ContentChangedArgs> ContentCleared = delegate { };
			internal Action<VisibilityChangedArgs> VisibilityChanged = delegate { };

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
					RaiseEvent();
					_content = value;

					void RaiseEvent()
					{
						var eventArgs = new ContentChangedArgs
						{
							CellColumn = Column,
							CellRow = Row,
							PreviousContent = Content,
							NewContent = value
						};
						if (value is null)
						{
							ContentCleared(eventArgs);
						}
						else
						{
							ContentChanged(eventArgs);
						}
					}
				}
			}
			Visibility _visibility;
			internal Visibility Visibility
			{
				get => _visibility;
				set
				{
					var eventArgs = new VisibilityChangedArgs
					{
						CellRow = Row,
						CellColumn = Column,
						Previous = _visibility,
						New = value
					};
					VisibilityChanged(eventArgs);
					_visibility = value;
				}
			}

			internal void ClearContent()
			{
				Content = null;
			}

			internal class VisibilityChangedArgs
			{
				internal int CellRow { get; init; }
				internal int CellColumn { get; init; }
				internal Visibility Previous { get; init; }
				internal Visibility New { get; init; }
			}
			internal class ContentChangedArgs
			{
				internal int CellRow { get; init; }
				internal int CellColumn { get; init; }
				internal UIElement PreviousContent { get; init; }
				internal UIElement NewContent { get; init; }
			}
		}
		internal class CellsEnumerable : IEnumerable<Cell>
		{
			readonly List<List<Cell>> rows;

			public CellsEnumerable(List<List<Cell>> rows)
			{
				this.rows = rows;
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
				int ColumnsCount => cells.rows.Count == 0 ? 0 : cells.rows[0].Count;

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
