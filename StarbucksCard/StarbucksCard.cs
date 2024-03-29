﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using RestSharp;
using RestSharp.Deserializers;

namespace StarbucksCard
{

    // all based on the PHP version at https://github.com/Neal/php-starbucks
    public class StarbucksCard
    {

        private string _username;
        private string _password;

        /// <summary>
        /// The card owner's name.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// The cumulative number of stars earned, presumably since you earned the latest level?
        /// </summary>
        public int CumulativeStars { get; set; }

        /// <summary>
        /// The number of rewards not yet redeemed.
        /// </summary>
        public int UnredeemedRewards { get; set; }

        /// <summary>
        /// The current card balance.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// The date and time the balance was last updated.
        /// </summary>
        public DateTime BalanceUpdated { get; set; }

        /// <summary>
        /// The number of stars earned since the card was registered.
        /// </summary>
        public int LifetimeStars { get; set; }

        /// <summary>
        /// The numeric level you're at - Black, Green, Gold...
        /// </summary>
        public int CustomerLevel { get; set; }

        /// <summary>
        /// The date the CustomerLevel was reached.
        /// </summary>
        public DateTime CustomerLevelSince { get; set; }

        /// <summary>
        /// The number of stars until your next free drink.
        /// </summary>
        public int StarsTillNextDrink { get; set; }

        /// <summary>
        /// The card owner's birthday (when they get their free birthday drink).
        /// </summary>
        public DateTime CustomerBirthDate { get; set; }

        /// <summary>
        /// The date the card was originally registered.
        /// </summary>
        public DateTime CardholderSince { get; set; }

        /// <summary>
        /// Whether or not Auto Reload is enabled for the card.
        /// </summary>
        public bool AutoReload { get; set; }

        /// <summary>
        /// The date your gold membership will renew (the date by which you have to have earned 30 stars).
        /// </summary>
        public DateTime GoldRenewDate { get; set; }

        /// <summary>
        /// I'm not sure if this is the number left to secure your renewal, or the number towards your renewal...
        /// We'll see when I get Gold again in a month or two.
        /// </summary>
        public int? StarsGoldStatus { get; set; }

        /// <summary>
        /// A textual version of the rewards level, based on the numeric CustomerLevel.
        /// </summary>
        public string RewardsLevel
        {
            get
            {
                switch (CustomerLevel)
                {
                    case 1:
                        return "Welcome";
                    case 2:
                        return "Green";
                    case 3:
                        return "Gold";
                    default:
                        return "Unknown";
                }
            }
        }

        /// <summary>
        /// All the rewards that have been earned, regardless of status (ie: redeemed, expired, etc.).
        /// 
        /// Be warned that this appears to sometimes have duplicates. My birthday reward is in the list twice, but UnredeemedRewards correctly shows only 1.
        /// </summary>
        public List<Reward> EarnedRewards { get; set; }

        public List<StarHistory> StarHistory { get; set; }

        public StarbucksCard(string username, string password)
        {
            this._username = username;
            this._password = password;

            EarnedRewards = new List<Reward>();
            StarHistory = new List<StarHistory>();
        }

        public void Update()
        {

            using (var client = new CustomWebClient())
            {
                var postValues = new NameValueCollection();
                postValues["Account.UserName"] = this._username;
                postValues["Account.PassWord"] = this._password;

                client.Cookies.Add(new Cookie("acceptscookies", "ok")
                {
                    Domain = ".starbucks.com"
                });

                var response = client.UploadValues("https://www.starbucks.com/account/signin", "POST", postValues);

                var responseString = Encoding.UTF8.GetString(response);

                this.ParseValues(responseString);
            }

        }

        private void ParseValues(string response)
        {

            CustomerName = ParseValue(response, "customer_full_name");
            CumulativeStars = Convert.ToInt16(ParseValue(response, "cumulative_star_balance"));
            UnredeemedRewards = Convert.ToInt16(ParseValue(response, "num_unredeemed_rewards"));
            Balance = Convert.ToDecimal(ParseValue(response, "card_dollar_balance"));

            var updated = ParseValue(response, "card_balance_date") + " " + ParseValue(response, "card_balance_time");

            BalanceUpdated = DateTime.Parse(updated);

            LifetimeStars = Convert.ToInt16(ParseValue(response, "lifetime_star_count"));
            CustomerLevel = Convert.ToInt16(ParseValue(response, "customer_level"));
            CustomerLevelSince = DateTime.Parse(ParseValue(response, "customer_level_since_date"));
            StarsTillNextDrink = Convert.ToInt16(ParseValue(response, "num_stars_till_next_drink"));
            CustomerBirthDate = DateTime.Parse(ParseValue(response, "customer_birth_date"));
            CardholderSince = DateTime.Parse(ParseValue(response, "cardholder_since_date"));
            AutoReload = Convert.ToBoolean(ParseValue(response, "auto_reload"));

            GoldRenewDate = DateTime.Parse(ParseValue(response, "renew_date_for_gold_status"));

            var starsGoldStatus = ParseValue(response, "renew_num_stars_gold_status");

            if (starsGoldStatus != "")
            {
                StarsGoldStatus = Convert.ToInt16(starsGoldStatus);
            }

            // this is a bit of a hilarious hack. we're going to use restsharp only to deserialize this
            // @todo parse out the JSON object in the page and have RestSharp parse the whole thing
            var restResponse = new RestResponse();
            restResponse.Content = ParseValue(response, "star_history");
            restResponse.ContentType = "application/json";

            var serializer = new JsonDeserializer();
            var history = serializer.Deserialize<List<StarHistory>>(restResponse);

            StarHistory = history;

            // we're doing the same thing here
            restResponse.Content = ParseValue(response, "customer_active_coupons");
            var earnedRewards = serializer.Deserialize<List<Reward>>(restResponse);

            EarnedRewards = earnedRewards;

        }

        private string ParseValue(string response, string key)
        {

            var pos = response.IndexOf(key);
            var startPos = pos + key.Length + ": ".Length;     // add our position to our delimiters to get the real start
            var stopPos = response.IndexOf("\r\n", startPos);      // find the newline at the end of the line

            // pull out the value
            var value = response.Substring(startPos, stopPos - startPos);

            // most of the values are strings, but some are not... so we'll trim it up now. also, the trailing comma, if there is one
            char[] toTrim = { '\'', ',' };
            value = value.Trim(toTrim);

            return value;

        }

    }

    public class StarHistory
    {
        /// <summary>
        /// Date the star was earned.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// The number of stars earned.
        /// </summary>
        public int Stars { get; set; }
        /// <summary>
        /// Type of event that earned the star(s). Most often it is "Purchases", but you get "Promotion", "GroceryPurchase", and "Bonus" as well.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// A textual explanation. This is usually not populated, it looks like it may only show up on "Bonus" type entries. For example: "2 Bonus Star - Frappuccino"
        /// </summary>
        public string Title { get; set; }
    }

    public class Reward
    {
        /// <summary>
        /// The type of voucher. So far seems to be "MSRPromotionalCoupon" for promotions or "MSREarnCoupon" for actual rewards.
        /// </summary>
        public string VoucherType { get; set; }
        /// <summary>
        /// The name of the coupon. Often this is cryptic ("Code 592-25% OFF FOOD"), but rewards make sense ("EARNED FREE DRINK" or "BIRTHDAY FREE BEVERAGE US").
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The actual code that is used in their system.
        /// </summary>
        public string CouponCode { get; set; }
        /// <summary>
        /// Obvious, the date the reward or coupon was issued.
        /// </summary>
        public DateTime DateIssued { get; set; }
        /// <summary>
        /// Obvious, the date the reward or coupon expires.
        /// </summary>
        public DateTime ExpirationDate { get; set; }
        /// <summary>
        /// The status of the reward. "Expired", "Redeemed", or "Available" seem to be used.
        /// </summary>
        public string Status { get; set; }
    }
}