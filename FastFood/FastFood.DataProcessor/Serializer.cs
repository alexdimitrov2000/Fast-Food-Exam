using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FastFood.Data;
using FastFood.DataProcessor.Dto.Export;
using FastFood.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FastFood.DataProcessor
{
	public class Serializer
	{
		public static string ExportOrdersByEmployee(FastFoodDbContext context, string employeeName, string orderType)
		{
            Employee employee = context.Employees
                .Include(e => e.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .First(e => e.Name == employeeName);

            var orders = employee.Orders
                .Where(o => o.Type.ToString() == orderType)
                .Select(o => new
                {
                    Customer = o.Customer,
                    Items = o.OrderItems.Select(oi => new
                    {
                        Name = oi.Item.Name,
                        Price = oi.Item.Price,
                        Quantity = oi.Quantity
                    }).ToArray(),
                    TotalPrice = o.TotalPrice
                })
                .OrderByDescending(o => o.TotalPrice)
                .ThenByDescending(o => o.Items.Length)
                .ToArray();

            var empOrders = new
            {
                Name = employee.Name,
                Orders = orders,
                TotalMade = orders.Sum(o => o.TotalPrice)
            };

            var serializedOrders = JsonConvert.SerializeObject(empOrders, Newtonsoft.Json.Formatting.Indented);

            return serializedOrders;
		}

		public static string ExportCategoryStatistics(FastFoodDbContext context, string categoriesString)
		{
            string[] categoryNames = categoriesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToArray();

            Category[] categories = context.Categories
                .Include(c => c.Items)
                .ThenInclude(i => i.OrderItems)
                .ThenInclude(oi => oi.Item)
                .Where(c => categoryNames.Any(cn => c.Name == cn)).ToArray();

            List<CategoryDto> categs = new List<CategoryDto>();

            foreach (var cat in categories)
            {
                var mostPopular = cat.Items
                    .Select(i => new
                    {
                        Name = i.Name,
                        TotalMade = i.OrderItems.Sum(oi => oi.Quantity * oi.Item.Price),
                        TimesSold = i.OrderItems.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(i => i.TotalMade)
                    .First();
                

                categs.Add(new CategoryDto
                {
                    Name = cat.Name,
                    MostPopularItem = new MostPopularItemDto
                    {
                        Name = mostPopular.Name,
                        TotalMade = mostPopular.TotalMade,
                        TimesSold = mostPopular.TimesSold
                    }
                });
            }

            categs = categs
                .OrderByDescending(c => c.MostPopularItem.TotalMade)
                .ThenByDescending(c => c.MostPopularItem.TimesSold)
                .ToList();

            var serializer = new XmlSerializer(typeof(List<CategoryDto>), new XmlRootAttribute("Categories"));
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            var stringWriter = new StringWriter();

            serializer.Serialize(stringWriter, categs, namespaces);

            return stringWriter.ToString();
		}
	}
}