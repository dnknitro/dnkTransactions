using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnkTransactionsUI.Domain;
using dnkTransactionsUI.Utils;

namespace dnkTransactionsUI.Service
{
	public class TransactionsService
	{
		public List<Transaction> LoadTransactions(string fileLocation, int? limitToYear = null)
		{
			var list = new List<Transaction>();
			if (!File.Exists(fileLocation))
				return list;
			const int dateColIndex = 0;
			const int descriptionColIndex = 1;
			const int originalDescriptionColIndex = 2;
			const int amountColIndex = 3;
			const int transactionTypeColIndex = 4;
			const int categoryTypeColIndex = 5;
			const int accountNameColIndex = 6;
			const int labelsColIndex = 7;

			foreach (var row in CSVUtils.ReadCSVFile(fileLocation).Skip(1))
			{
				var date = DateTime.Parse(row[dateColIndex]);
				if (limitToYear != null && date.Year != limitToYear.Value) continue;
				var transaction = new Transaction
				{
					Date = date,
					Description = row[descriptionColIndex],
					OriginalDescription = row[originalDescriptionColIndex],
					AmountMod = decimal.Parse(row[amountColIndex]),
					Type = row[transactionTypeColIndex] == "debit" ? TransactionType.Debit : TransactionType.Credit,
					Category = row[categoryTypeColIndex],
					AccountName = row[accountNameColIndex],
					Labels = row[labelsColIndex]
				};
				list.Add(transaction);
			}

			list = list.OrderBy(x => x.Date).ThenBy(x => x.AmountMod).ToList();

			var turnaroundGroup = 1;
			for (int i = 0; i < list.Count - 1; i++)
			{
				var transaction1 = list[i];
				if (transaction1.TurnaroundGroup != null) continue;

				for (int j = i + 1; j < list.Count; j++)
				{
					var transaction2 = list[j];

					if ((transaction2.Date - transaction1.Date).TotalDays > 120) break;

					if (transaction2.TurnaroundGroup == null
					    && transaction1.AmountMod == transaction2.AmountMod
					    && transaction1.Category == transaction2.Category
					    && transaction1.Type != transaction2.Type)
					{
						transaction1.TurnaroundGroup = transaction2.TurnaroundGroup = turnaroundGroup++;
						break;
					}
				}
			}

			return list;
		}
	}
}