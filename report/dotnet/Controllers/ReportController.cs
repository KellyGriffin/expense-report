﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Report.Models;
using Expense.Client;
using System.Collections.Generic;
using Expense.Models;
using Toggle;

namespace expense.Controllers
{
  [Route("api/[controller]")]
  [ApiController]

  public class ReportController : ControllerBase
  {
    private readonly IExpenseClient _client;
    private readonly IToggleClient _toggleClient;

    public ReportController(IExpenseClient client, IToggleClient toggleClient)
    {
      _client = client;
      _toggleClient = toggleClient;
    }

    [HttpGet("expense/version")]
    public async Task<ActionResult<string>> GetVersion()
    {
      return await _client.GetExpenseVersion();
    }

    [HttpGet("trip/{id}")]
    public async Task<ActionResult<ReportTotal>> GetReportForTrip(string id)
    {
      var items = await _client.GetExpensesForTrip(id);
      List<ExpenseItem> copied = new List<ExpenseItem>(items);
      var report = CreateReport(id, copied);
      if (report == null)
      {
        return NotFound();
      }
      return report;
    }

    private ReportTotal CreateReport(string tripId, IList<ExpenseItem> items)
    {
      decimal total = getTotal(items);

      ReportTotal reportTotal = new ReportTotal
      {
        TripId = tripId,
        Total = total,
        Expenses = items
      };

      addNumItems(reportTotal);
      addTotalReimbursable(reportTotal, items);

      return reportTotal;
    }

    private decimal getTotal(IList<ExpenseItem> items)
    {
      decimal total = 0;
      foreach (ExpenseItem item in items)
      {
        total += item.Cost;
      }
      return total;
    }

    private decimal getTotalReimbursable(IList<ExpenseItem> items) {
      decimal reimbursable = 0;
      foreach (ExpenseItem item in items)
      {
        if (item.Reimbursable == true)
        {
          reimbursable += item.Cost;
        }
      }
      return reimbursable;
    }

    private void addNumItems(ReportTotal reportTotal)
    {
      if (_toggleClient.GetToggleValue("enable-number-of-items").Result)
      {
        reportTotal.NumberOfExpenses = reportTotal.Expenses.Count;
      }
    }

    private void addTotalReimbursable(ReportTotal reportTotal, IList<ExpenseItem> items)
    {
      if (_toggleClient.ToggleForExperiment("expense").Result) {
        reportTotal.TotalReimbursable = getTotalReimbursable(items);;
      }
    }
  }
}