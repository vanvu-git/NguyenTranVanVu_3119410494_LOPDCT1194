using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pay1193.Entity;
using Pay1193.Services.Implement;
using Pay1193.Services;
using System.Collections;
using System.Data;
using Pay1193.Models;

namespace Pay1193.Controllers
{
    public class PaymentController : Controller
    {

        private readonly IPayService _payService;
        private readonly IEmployee _employeeService;
        private readonly ITaxService _taxService;
        private readonly INationalInsuranceService _nationalInsuranceService;
        private decimal overtimeHrs;
        private decimal contractualEarnings;
        private decimal overtimeEarnings;
        private decimal totalEarnings;
        private decimal tax;
        private decimal unionFee;
        private decimal studentLoan;
        private decimal nationalInsurance;
        private decimal totalDeduction;

        public PaymentController(IPayService payService, IEmployee employeeService, ITaxService taxService, INationalInsuranceService nationalInsuranceService)
        {
            _payService = payService;
            _employeeService = employeeService;
            _taxService = taxService;
            _nationalInsuranceService = nationalInsuranceService;

        }

        public IActionResult Index()
        {
           
            var payRecords = _payService.GetAll().Select(pay => new PaymentRecordIndexViewModel
            {
                
                Id = pay.Id,
                FullName = _employeeService.GetById(pay.EmployeeId).FullName,
                EmployeeId = pay.EmployeeId,
                PayDate = pay.DatePay,
                PayMonth = pay.MonthPay,
                TaxYearId = pay.TaxYearId,
                Year = _payService.GetTaxYearById(pay.TaxYearId).YearOfTax,
                TotalEarnings = pay.TotalEarnings,
                NetPayment = pay.NetPayment,
                TotalDeduction = pay.EarningDeduction,
                Employee = pay.Employee
            }); 
            return View(payRecords);
        }
        [HttpGet]
        public IActionResult Create()
        {
            
            ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.taxYears = _payService.GetAllTaxYear();
            var model = new PaymentRecordCreateViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentRecordCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var payrecord = new PaymentRecord()
                {
                    Id = model.Id,
                    EmployeeId = model.EmployeeId,
                    TaxYearId = model.TaxYearId,
                    DatePay = model.PayDate,
                    MonthPay = model.PayMonth,
                    TaxCode = model.TaxCode,
                    HourlyRate = model.HourlyRate,
                    HourWorked = model.HoursWorked,
                    ContractualHours = model.ContractualHours,
                    OvertimeHours = overtimeHrs = _payService.OverTimeHours(model.HoursWorked, model.ContractualHours),
                    ContractualEarnings = contractualEarnings = _payService.ContractualEarning(model.ContractualHours, model.HoursWorked, model.HourlyRate),
                    OvertimeEarnings = overtimeEarnings = _payService.OvertimeEarnings(_payService.OvertimeRate(model.HourlyRate), overtimeHrs),
                    TotalEarnings = totalEarnings = _payService.TotalEarnings(overtimeEarnings, contractualEarnings),
                    Tax = tax = _taxService.TaxAmount(totalEarnings),
                    UnionFee = unionFee = _employeeService.UnionFee(model.EmployeeId),
                    SLC = studentLoan = _employeeService.StudentLoanRepaymentAmount(model.EmployeeId, totalEarnings),
                    NiC = nationalInsurance = _nationalInsuranceService.NIContribution(totalEarnings),
                    EarningDeduction = totalDeduction = _payService.TotalDeduction(tax, nationalInsurance, studentLoan, unionFee),
                    NetPayment = _payService.NetPay(totalEarnings, totalDeduction)
                };
                await _payService.CreateAsync(payrecord);
                return RedirectToAction("Index");
            }
            ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.taxYears = _payService.GetAllTaxYear();
            return View(model);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var paymentrecord = _payService.GetById(id);
            if (paymentrecord == null)
            {
                return NotFound();
            }
            var model = new EmployeeDeleteViewModel
            {
                Id = paymentrecord.Id
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(PaymentRecordDeleteViewModel model)
        {
            await _payService.Delete(model.Id);
            return RedirectToAction("Index");
        }

    }
}