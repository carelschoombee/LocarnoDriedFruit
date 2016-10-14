﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Models
{
    public class Dashboard
    {
        public SalesInfo SalesInformation { get; set; }

        public List<OrderHeader> ConfirmedOrders { get; set; }

        [DisplayName("Time Period")]
        [Required]
        public string SelectedYear { get; set; }
        public List<SelectListItem> YearOptions { get; set; }

        [DisplayName("Time Period")]
        [Required]
        public DateTime SelectedMonth { get; set; }

        public string Period { get; set; }

        public Dashboard()
        {

        }

        public Dashboard(string year, string period)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                DateTime minDate = (DateTime)db.OrderHeaders.Min(c => c.OrderDatePlaced);
                DateTime maxDate = DateTime.Now;

                int i = minDate.Year;

                if (!string.IsNullOrEmpty(year))
                    SelectedYear = year;
                else
                    SelectedYear = DateTime.Now.Year.ToString();

                YearOptions = new List<SelectListItem>();

                YearOptions.Add(new SelectListItem
                {
                    Text = "Since Inception",
                    Value = "0"
                });

                while (i <= maxDate.Year)
                {
                    YearOptions.Add(new SelectListItem
                    {
                        Text = i.ToString(),
                        Value = i.ToString(),
                        Selected = (i.ToString() == SelectedYear ? true : false)
                    });
                    i++;
                }

                Period = period;
                SelectedMonth = DateTime.Now;
                SalesInformation = new SalesInfo(int.Parse(SelectedYear));
                ConfirmedOrders = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed").ToList();
            }
        }

        public Dashboard(DateTime date, string period)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                DateTime minDate = (DateTime)db.OrderHeaders.Min(c => c.OrderDatePlaced);
                DateTime maxDate = DateTime.Now;

                int i = minDate.Year;

                SelectedYear = null;

                YearOptions = new List<SelectListItem>();

                YearOptions.Add(new SelectListItem
                {
                    Text = "Since Inception",
                    Value = "0"
                });

                while (i <= maxDate.Year)
                {
                    YearOptions.Add(new SelectListItem
                    {
                        Text = i.ToString(),
                        Value = i.ToString(),
                        Selected = (i.ToString() == SelectedYear ? true : false)
                    });
                    i++;
                }

                Period = period;
                SelectedMonth = date;
                SalesInformation = new SalesInfo(date);
                ConfirmedOrders = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed").ToList();
            }
        }
    }
}