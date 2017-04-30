using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace shopify_backend_challenge
{

	public class ExtraCookies
	{
		public int remaining_cookies { get; set; }
		public List<int> unfulfilled_orders { get; set; }
		public ExtraCookies(int remainingAmount, List<int> unfulfilledOrders)
		{
			this.remaining_cookies = remainingAmount;
			this.unfulfilled_orders = unfulfilledOrders;
		}
	}

	public class Rootobject
	{
		public int available_cookies { get; set; }
		public Order[] orders { get; set; }
		public Pagination pagination { get; set; }
	}

	public class Pagination
	{
		public int current_page { get; set; }
		public int per_page { get; set; }
		public int total { get; set; }
	}

	public class Order
	{
		public int id { get; set; }
		public bool fulfilled { get; set; }
		public string customer_email { get; set; }
		public Product[] products { get; set; }
	}

	public class Product
	{
		public string title { get; set; }
		public int amount { get; set; }
		public float unit_price { get; set; }
	}



	class Program
	{

		const string ApiUrl = "https://backend-challenge-fall-2017.herokuapp.com/orders.json";

		static void Main(string[] args)
		{
			int availableCookies = 0;
			var orders = GetOrders(ApiUrl, out availableCookies);
			var cookieOrders = GetCookieOrders(orders);
			GenerateOutput(cookieOrders, availableCookies);

			//Console.ReadKey();
		}

		/// <summary>
		/// get the response of the url containing the json data
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		static async Task<string> GetUrlData(string url)
		{
			var client = new HttpClient();
			var response = await client.GetAsync(url);
			var content = await response.Content.ReadAsStringAsync();
			return content;
		}

		/// <summary>
		/// gets the list of orders from a URL containing a json file
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		static List<Order> GetOrders(string url, out int availableCookies)
		{
			int page = 1;
			var orders = new List<Order>();
			availableCookies = 0;

			while (true)
			{
				var pageUrl = url + "?page=" + page;
				var jsonObject = JsonConvert.DeserializeObject<Rootobject>(GetUrlData(pageUrl).Result);

				//keep increasing the page number until a page is reached that contains no orders
				//since in the API the page can increase forver but will display a page without orders
				if (jsonObject.orders == null || jsonObject.orders.ToList().Count == 0) break;

				availableCookies = jsonObject.available_cookies;

				//made a list of all the orders across every page
				orders.AddRange(jsonObject.orders);
				page++;
			}
			return orders;

		
		}

		/// <summary>
		/// filters the orders by the ones that are unfulfilled and have cookies, then sorts them by most cookies first
		/// </summary>
		/// <param name="orders"></param>
		/// <returns></returns>
		static List<Order> GetCookieOrders(List<Order> orders)
		{
			var cookieOrders = new List<Order>();

			//get all the orders that contain cookies and haven't been fulfilled yet
			cookieOrders.AddRange(
					orders.Where(
						order => order.products.Where(product => product.title.Equals("Cookie")).ToList().Count > 0
					).Where(o => o.fulfilled == false).ToList()
				);

			//get a list of the number of cookies each order has
			var cookieAmountsPerOrder =
				cookieOrders.SelectMany(
					o => o.products.Where(p => p.title.Equals("Cookie"))
					).Select(p => p.amount).ToList();

			//sort cookieOrders by highest number of cookies left using the generated list of number of cookies per order.
			cookieOrders = cookieOrders.Zip(cookieAmountsPerOrder, (o, a) => new { order = o, amount = a })
				.OrderByDescending(pair => pair.amount)
				.Select(pair => pair.order).ToList();

			return cookieOrders;
		}

		/// <summary>
		/// generates the json file from the list of sorted cookie orders
		/// </summary>
		/// <param name="cookieOrders"></param>
		/// <param name="availableCookies"></param>
		static void GenerateOutput(List<Order> cookieOrders, int availableCookies)
		{
			var unfulfilledOrders = new List<int>();
			foreach (var order in cookieOrders)
			{
				//each order can only have 1 cookie product, so just get the first one from the enumerable result
				var orderCookieAmount = order.products.Where(p => p.title.Equals("Cookie")).Select(p=>p.amount).First();
				if(orderCookieAmount > availableCookies)
				{
					unfulfilledOrders.Add(order.id);
				}
				else
				{
					availableCookies -= orderCookieAmount;
				}
			}
			var extraCookies = new ExtraCookies(availableCookies, unfulfilledOrders);

			//serialize extraCookies into a json file
			JsonSerializer serializer = new JsonSerializer();
			serializer.NullValueHandling = NullValueHandling.Ignore;
			using (StreamWriter stream = new StreamWriter(Directory.GetCurrentDirectory() + @"/output.json")) 
			using(JsonWriter writer = new JsonTextWriter(stream))
			{
				serializer.Serialize(writer, extraCookies);
			}
		}
	}



}
