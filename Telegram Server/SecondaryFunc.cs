using Newtonsoft.Json;
using static Program.TelegramBot;
using static Program.Keyboard;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using static Program.ResponseFromYandexMaps;

namespace Program
{
    class Secondaryfunctions
    {
        public static string searchorganizations(string organization, (double, double) coordinates)
        {
            string buff = "";
            string language;
            if (database[userid].language == "en")
            {
                language = "en_RU";
            }
            else if (database[userid].language == "ru")
            {
                language = "ru_RU";
            }
            else language = "en_RU";
            double bias = settings!.kilometerstolerance! / 111.134861111;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Uri address = new Uri($"https://search-maps.yandex.ru/v1/?text={organization}&bbox={coordinates.Item2},{coordinates.Item1}~{coordinates.Item2 + bias},{coordinates.Item1 + bias}&type=biz&lang={language}&results={settings!.searchresultsarea!}&apikey={settings!.yandexmaptoken!}");
            Console.WriteLine(address);
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (WebClient client = new WebClient())
            {
                try
                {
                    database[userid]!.listofrecentsearchedplaces!.Clear();
                    client.Encoding = System.Text.Encoding.UTF8;
                    string request = client.DownloadString(address);
                    Rootobject answer = JsonConvert.DeserializeObject<ResponseFromYandexMaps.Rootobject>(request)!;
                    buff += "-------------------------------------\n";
                    foreach (var feature in answer!.features)
                    {
                        database[userid]!.listofrecentsearchedplaces!.Add((feature.geometry.coordinates[1], feature.geometry.coordinates[0], feature.properties.CompanyMetaData.name, feature.properties.CompanyMetaData.address)!);
                        if (feature.properties.CompanyMetaData.name != null) buff += $"➡️{organization}: <b>\"{feature.properties.CompanyMetaData.name}\"</b>\n";
                        if (feature.properties.CompanyMetaData.address != null) buff += $"🗺️<b>{botword["addresstext"]}</b> <i>{feature.properties.CompanyMetaData.address}</i> \n📞<b>{botword["phonenumberstext"]}</b>\n";
                        if (feature.properties.CompanyMetaData.Phones != null) foreach (var formatted in feature.properties.CompanyMetaData.Phones) buff += $"          <i>{formatted.formatted}</i>\n";
                        if (feature.properties.CompanyMetaData.Hours.text != null) buff += $"📅<b>{botword["operatingscheduletext"]}</b> <i>{feature.properties.CompanyMetaData.Hours.text}</i>\n";
                        if (feature.properties.CompanyMetaData.url != null) buff += $"🌐<b>Сайт</b>: {feature.properties.CompanyMetaData.url}\n";
                        buff += "-------------------------------------\n";
                    }
                }
                catch
                {
                }
            }
            return buff;

        }
        public static InlineKeyboardMarkup inlinepreparationroutebuttons(List<(float, float, string, string)>? listofrecentsearchedplaces)
        {

            List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < listofrecentsearchedplaces!.Count()!; ++i)
            {
                InlineKeyboardButton button = new InlineKeyboardButton(listofrecentsearchedplaces![i].Item3) { CallbackData = "geolocation" + i };
                InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                list.Add(row);
            }
            var inlinedescriptiondiseaseen = new InlineKeyboardMarkup(list);
            return inlinedescriptiondiseaseen;
        }
        public static InlineKeyboardMarkup inlinepreparationdescriptiondiseases()
        {

            InlineKeyboardMarkup inlinedescriptiondiseaseen = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: botword["descriptiondisease"]+botword["d"+database[userid].listofrecentdiseases![0]].Substring(3, botword["d"+database[userid].listofrecentdiseases![0]].Length - 7), callbackData: "description1"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: botword["descriptiondisease"]+botword["d"+database[userid].listofrecentdiseases![1]].Substring(3, botword["d"+database[userid].listofrecentdiseases![1]].Length - 7), callbackData: "description2"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: botword["descriptiondisease"]+botword["d"+database[userid].listofrecentdiseases![2]].Substring(3, botword["d"+database[userid].listofrecentdiseases![2]].Length - 7), callbackData: "description3"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: botword["descriptiondisease"]+botword["d"+database[userid].listofrecentdiseases![3]].Substring(3, botword["d"+database[userid].listofrecentdiseases![3]].Length - 7), callbackData: "description4"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: botword["descriptiondisease"]+botword["d"+database[userid].listofrecentdiseases![4]].Substring(3, botword["d"+database[userid].listofrecentdiseases![4]].Length - 7), callbackData: "description5"),
                }
            });
            return inlinedescriptiondiseaseen;
        }

        public static string cantileverstrip(int percent)
        {
            char[] stripfull = new char[] { '░', '░', '░', '░', '░', '░', '░', '░', '░' };
            for (int i = 0; i < percent / 10; ++i)
            {
                stripfull[i] = '█';
            }
            string strip = new string(stripfull);
            return strip;
        }

        public static Dictionary<string, string> BotwordDictpreparer(Dictionary<string, string> botword, string path)
        {
            Textbot? textbot = JsonConvert.DeserializeObject<Textbot>(File.ReadAllText(@path));
            for (int i = 0; i < textbot!.Textforbot!.Length; i++)
            {
                botword.TryAdd(textbot.Textforbot[i].TextName, textbot.Textforbot[i].Text);
            }
            return botword;
        }

        public static Dictionary<long, User> DatabaseDictFillFromJSON(string path)
        {
            Dictionary<long, User>? data = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText(@path));

            return data!;
        }



        public static void DatabaseDictSaverToJSON(Dictionary<long, User> database, string path)
        {

            File.WriteAllText(@path, JsonConvert.SerializeObject(database, Formatting.Indented));
        }

        public static string symptomhandler(List<int> select)
        {
            string symptomsselected = "";

            for (int i = 0; i < select.Count; i++)
            {
                symptomsselected += botword[select[i].ToString()];
                symptomsselected += botword[select[i] + "categoryofdiseases"];
            }
            Console.WriteLine(symptomsselected);
            return symptomsselected;


        }
        public static void interfacelocalization(string language)
        {
            if (language == "ru")
            {
                botword = botwordru;
                welcomkeyboard = welcomkeyboardru;
                symptomkeyboard = symptomkeyboardru;
                inlineKeyboard = inlineKeyboardru;
                inlinegenderkeyboard = inlinegenderkeyboardru;
                geolocationkeyboard = geolocationkeyboardru;

            }
            else
            {
                botword = botworden;
                welcomkeyboard = welcomkeyboarden;
                symptomkeyboard = symptomkeyboarden;
                inlineKeyboard = inlineKeyboarden;
                inlinegenderkeyboard = inlinegenderkeyboarden;
                geolocationkeyboard = geolocationkeyboarden;

            }

        }


    }
}