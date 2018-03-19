using System;

namespace dnkTransactionsUI.Domain
{
	public enum TransactionType
	{
		Debit,
		Credit
	}

	public class Transaction
	{
		public int Year => Date.Year;
		public int Month => Date.Month;
		public DateTime Date { get; set; }
		public string Description { get; set; }
		public string OriginalDescription { get; set; }
		public decimal Amount => Type == TransactionType.Debit ? -AmountMod : AmountMod;
		public decimal AmountMod { get; set; }
		public TransactionType Type { get; set; }
		public string Category { get; set; }
		public string AccountName { get; set; }
		public string Labels { get; set; }
		public string Notes { get; set; }

		public string RowBgColor
		{
			get
			{
				var color = Type == TransactionType.Credit ? "#ccffcc" : null;
				if (TurnaroundGroup != null) color = "#ffffb3";
				return color;
			}
		}

		public int? TurnaroundGroup { get; set; }
	}
}