﻿using System;
using System.Linq;
using ShareInvest.Catalog;

namespace ShareInvest.Strategy.XingAPI
{
    public class Heavy : StrategicChoice
    {
        double Max(double max, Classification classification)
        {
            var num = 4.5D;

            foreach (var kv in API.Judge)
                switch (classification)
                {
                    case Classification.Sell:
                        if (kv.Value > 0)
                            num += 0.5;

                        break;

                    case Classification.Buy:
                        if (kv.Value < 0)
                            num += 0.5;

                        break;
                }
            return max * num * 0.1;
        }
        protected internal override bool ForTheLiquidationOfBuyOrder(string price, double[] selling)
        {
            if (double.TryParse(price, out double sAvg) && sAvg == selling[selling.Length - 1] && SendNewOrder(price, sell))
            {
                var cPrice = sAvg - Const.ErrorRate;
                var oPrice = cPrice.ToString("F2");

                if (API.BuyOrder.Count == 0 && API.Quantity + 1 < specify.Assets / (cPrice * Const.TransactionMultiplier * specify.MarginRate))
                    return SendNewOrder(oPrice, buy);

                if (API.BuyOrder.Count > 0 && API.BuyOrder.ContainsValue(cPrice) == false)
                    return SendCorrectionOrder(oPrice, API.BuyOrder.OrderBy(o => o.Value).First().Key);
            }
            return false;
        }
        protected internal override bool ForTheLiquidationOfSellOrder(string price, double[] bid)
        {
            if (double.TryParse(price, out double bAvg) && bAvg == bid[bid.Length - 1] && SendNewOrder(price, buy))
            {
                var cPrice = bAvg + Const.ErrorRate;
                var oPrice = cPrice.ToString("F2");

                if (API.SellOrder.Count == 0 && Math.Abs(API.Quantity - 1) < specify.Assets / (cPrice * Const.TransactionMultiplier * specify.MarginRate))
                    return SendNewOrder(oPrice, sell);

                if (API.SellOrder.Count > 0 && API.SellOrder.ContainsValue(cPrice) == false)
                    return SendCorrectionOrder(oPrice, API.SellOrder.OrderByDescending(o => o.Value).First().Key);
            }
            return false;
        }
        protected internal override bool ForTheLiquidationOfBuyOrder(double[] selling)
        {
            var number = API.SellOrder.OrderBy(o => o.Value).First().Key;

            return API.SellOrder.TryGetValue(number, out double csp) && selling[API.SellOrder.Count == 1 ? 3 : (selling.Length - 1)] < csp ? SendClearingOrder(number) : false;
        }
        protected internal override bool ForTheLiquidationOfSellOrder(double[] bid)
        {
            var number = API.BuyOrder.OrderByDescending(o => o.Value).First().Key;

            return API.BuyOrder.TryGetValue(number, out double cbp) && bid[API.BuyOrder.Count == 1 ? 3 : (bid.Length - 1)] > cbp ? SendClearingOrder(number) : false;
        }
        protected internal override bool SetCorrectionBuyOrder(string avg, double buy)
        {
            var price = string.Empty;
            var gap = double.TryParse(avg, out double bAvg) && bAvg > buy ? Const.ErrorRate * 2 : Const.ErrorRate * 3;

            foreach (var kv in API.BuyOrder.OrderByDescending(o => o.Value))
            {
                if (string.IsNullOrEmpty(price) == false && (kv.Value + Const.ErrorRate).ToString("F2").Equals(price))
                    return SendCorrectionOrder((API.BuyOrder.OrderBy(o => o.Value).First().Value - gap).ToString("F2"), kv.Key);

                price = kv.Value.ToString("F2");
            }
            return false;
        }
        protected internal override bool SetCorrectionSellOrder(string avg, double sell)
        {
            var price = string.Empty;
            var gap = double.TryParse(avg, out double sAvg) && sAvg < sell ? Const.ErrorRate * 2 : Const.ErrorRate * 3;

            foreach (var kv in API.SellOrder.OrderBy(o => o.Value))
            {
                if (string.IsNullOrEmpty(price) == false && (kv.Value - Const.ErrorRate).ToString("F2").Equals(price))
                    return SendCorrectionOrder((API.SellOrder.OrderByDescending(o => o.Value).First().Value + gap).ToString("F2"), kv.Key);

                price = kv.Value.ToString("F2");
            }
            return false;
        }
        protected internal override bool SendNewOrder(double[] param, string classification)
        {
            var check = classification.Equals(buy);
            var price = param[check ? param.Length - 1 : 0];

            if (check && API.Quantity < 0 && param[5] > 0 && API.BuyOrder.Count < -API.Quantity && API.BuyOrder.ContainsValue(param[5]) == false)
                return SendNewOrder(param[5].ToString("F2"), buy);

            else if (check == false && API.Quantity > 0 && param[4] > 0 && API.SellOrder.Count < API.Quantity && API.SellOrder.ContainsValue(param[4]) == false)
                return SendNewOrder(param[4].ToString("F2"), sell);

            return price > 0 && GetPermission(price) && (check ? API.Quantity + API.BuyOrder.Count : API.SellOrder.Count - API.Quantity) < Max(specify.Assets / (price * Const.TransactionMultiplier * specify.MarginRate), check ? Classification.Buy : Classification.Sell) && (check ? API.BuyOrder.ContainsValue(price) : API.SellOrder.ContainsValue(price)) == false ? SendNewOrder(price.ToString("F2"), classification) : false;
        }
        bool GetPermission(double price)
        {
            if (API.BuyOrder.Count > 0 && API.Quantity > 0)
            {
                var cPrice = (price - Const.ErrorRate).ToString("F2");

                if (double.TryParse(cPrice, out double bPrice) && bPrice > API.BuyOrder.OrderBy(o => o.Value).First().Value && API.BuyOrder.ContainsValue(bPrice))
                    return false;
            }
            if (API.SellOrder.Count > 0 && API.Quantity < 0)
            {
                var cPrice = (price + Const.ErrorRate).ToString("F2");

                if (double.TryParse(cPrice, out double sPrice) && sPrice < API.SellOrder.OrderByDescending(o => o.Value).First().Value && API.SellOrder.ContainsValue(sPrice))
                    return false;
            }
            return true;
        }
        public Heavy(Catalog.XingAPI.Specify specify) : base(specify) => API.OnReceiveBalance = false;
    }
}