using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;

namespace BestStoreApi.Services
{
    public class OrderHelper
    {
        public static decimal ShippingFee { get; } = 5;

        public static Dictionary<string, string> PaymentMethods { get; } = new()
        {
             //{name,value}
            {"Cash","Cash on Delivery" },
            {"Paypal","Paypal" },
            {"Credit Card","Credit Card" },
        };

        public static List<string> PaymentStatus { get; } = new()
        {
            "Pending","Accepted","Canceled"
        };

        public static List<string> OrderStatus { get; } = new()
        {
            "Created","Accepted","Canceled",  "shipped","Delivered","Returned",
        };


        /*
         * Receives a string of product Identifiers, separated by('-')
         * Example : 9-9-7-9-6
         * returns a list of pairs (dictionary ):
         *      - the pair name is product Id
         *      - the pair value is product quantity
         * example
         * {
         *      9:3,
         *      7:1,
         *      6:1
         * }     
        */
        public static Dictionary<int, int> GetProductDictionary(string productIdentifiers)
        {
            var productDic = new Dictionary<int, int>();
            if (productIdentifiers.Length > 0)
            {
                string[] productIdArray = productIdentifiers.Split('-');
                foreach (string productId in productIdArray)
                {
                    try
                    {
                        int id = int.Parse(productId);
                        if (!productDic.TryAdd(id, 1))
                        {
                            productDic[id] += 1;
                        }
                    }
                    catch (Exception)
                    {
                        //ignored
                       
                    }
                }

            }
            return productDic;
        }
    }
}
