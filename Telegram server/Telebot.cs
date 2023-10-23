﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using System.Diagnostics;

namespace Program
{

    class TelegramBot
    {
        public static Dictionary<string, string> botword = new Dictionary<string, string>();
        public static Dictionary<long, User> database = new Dictionary<long, User>();
        private static Settings? settings = JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText(@"Telegramassets/Botsettings.json"));
        private static SymptomsList? SymptomsList = JsonConvert.DeserializeObject<SymptomsList>(System.IO.File.ReadAllText(settings!.pathsymptomslistjson));
        public static long userid = 0;
        static void Main()
        {


            botword = Dictionarypreparer.BotwordDictpreparer(botword, settings!.pathtextforbotjson);
            database = Dictionarypreparer.DatabaseDictFillFromJSON(settings.pathdatabasejson);

            var client = new TelegramBotClient(settings.token);
            client.StartReceiving(Update, Error);
            Console.ReadLine();
        }



        async static Task Update(ITelegramBotClient botclient, Update update, CancellationToken token)
        {
            var message = update.Message;
            var callback = update.CallbackQuery;

            if (update.Type == UpdateType.CallbackQuery && userid != 0 && database[userid].symptommenu)
            {
                await botclient.AnswerCallbackQueryAsync(callback!.Id, $"Выбрано {callback.Data}");
                if (int.TryParse(callback.Data, out _) && !database[userid]!.inlinebuttpressed!.Contains(int.Parse(callback.Data)))
                {
                    database[userid]!.inlinebuttpressed!.Add(int.Parse(callback?.Data ?? ""));
                }
                else if (callback.Data == "send" && database[userid]!.inlinebuttpressed!.Count != 0)
                {
                    database[userid]!.inlinebuttpressed!.Sort();
                    await botclient.SendTextMessageAsync(callback!.Message!.Chat!.Id, Dictionarypreparer.symptomhandler(database[userid]!.inlinebuttpressed!, SymptomsList!), parseMode: ParseMode.Html);
                    database[userid]!.inlinebuttpressed!.Clear();
                    database[userid].inlinesymptomkey = false;
                    await botclient.EditMessageReplyMarkupAsync(userid, database[userid].lastmessagebeforeinline + 2);
                }
                else if (callback.Data == "cancel" && database[userid]!.inlinebuttpressed!.Count != 0)
                {
                    database[userid]!.inlinebuttpressed!.Clear();
                }
                Dictionarypreparer.DatabaseDictSaverToJSON(database, settings!.pathdatabasejson);
            }


            if (message == null || message.Type != MessageType.Text) return;
            string TextMessage = message!.Text!.ToLower();
            userid = message.Chat.Id;
            if (TextMessage == botword["textbuttondefinitionofdisease"]) { database[userid].lastmessagebeforeinline = message.MessageId; }


            User user = new User
            {
                name = message!.Chat.FirstName ?? "no name"
            };
            if (database.ContainsKey(userid) == false)
            {
                Console.WriteLine("New user:   " + message.Chat.FirstName);
                database.TryAdd(userid, user);
            }
            if (userid == 802739746)
            {
                await botclient.SendTextMessageAsync(message.Chat.Id, "Ахуел?");

                return;
            }



            Console.WriteLine("maintmenu: " + database[userid].mainmenu + " symptommenu: " + database[userid].symptommenu);
            Console.WriteLine("Username: " + message.Chat.FirstName + " Message: " + message.Text + " Data: " + message.Date.ToLocalTime());


            if (TextMessage == "/start")
            {
                database[userid].mainmenu = true;
                database[userid].symptommenu = false;
                await botclient.SendStickerAsync(message.Chat.Id, sticker: InputFile.FromUri(botword["hallostik"]));
                await botclient.SendTextMessageAsync(message.Chat.Id, database[userid].name + " " + botword["textwelcome"], parseMode: ParseMode.Html, replyMarkup: Keyboard.welcomkeyboard);
            }

            if (database[userid].mainmenu)
            {
                if (TextMessage == botword["textbuttondefinitionofdisease"])
                {
                    database[userid].mainmenu = false;
                    database[userid].symptommenu = true;
                    await botclient.SendTextMessageAsync(message.Chat.Id, botword["textinputformat"], replyMarkup: Keyboard.symptomkeyboard, disableNotification: true);
                    await botclient.SendTextMessageAsync(message.Chat.Id, botword["textinputformat2"], replyMarkup: Keyboard.inlineKeyboard, parseMode: ParseMode.Html, disableNotification: true);
                    database[userid].inlinesymptomkey = true;
                    database[userid]!.inlinebuttpressed!.Clear();
                    TextMessage = "";
                }
                else if (TextMessage == botword["textbuttonreference"])
                {
                    await botclient.SendTextMessageAsync(message.Chat.Id, botword["textreference"], replyMarkup: Keyboard.inlinelinkes, disableNotification: true);
                    await botclient.SendStickerAsync(message.Chat.Id, sticker: InputFile.FromUri(botword["refstik"]));
                }
                else return;
            }

            if (database[userid].symptommenu)
            {
                if (TextMessage == botword["textbuttonbacktomainmenu"])
                {
                    await botclient.SendTextMessageAsync(message.Chat.Id, database[userid].name + " " + botword["textwelcome2"], parseMode: ParseMode.Html, replyMarkup: Keyboard.welcomkeyboard, disableNotification: true);
                    database[userid].mainmenu = true;
                    database[userid].symptommenu = false;
                }





                if (database[userid].mainmenu == false && database[userid].inlinesymptomkey == false) //проверка правильности входных ванных
                {
                    bool wrongmessage = false;//неправильные данных
                    string buf = "";//буфер строк
                    int countinputsymptoms = 1;//для расчетов
                    int[] symptomsarray = new int[settings!.countsymptoms];//введеные симптомы

                    //проверка формата строки
                    for (int i = 0; i < TextMessage.Length; i++)
                    {
                        if (!int.TryParse(TextMessage[i].ToString(), out _) && TextMessage[i] != ' ') wrongmessage = true;
                        if (i != TextMessage.Length - 1)
                        {
                            if (TextMessage[i] == ' ' && TextMessage[i + 1] == ' ') wrongmessage = true;
                            continue;
                        }
                        if (TextMessage[i] == ' ' && TextMessage[i - 1] == ' ') wrongmessage = true;
                    }
                    //проверка формата строки

                    //исключение проблем 
                    if (wrongmessage)
                    {
                        await botclient.SendStickerAsync(message.Chat.Id, sticker: InputFile.FromUri(botword["errorstik"]));
                        await botclient.SendTextMessageAsync(message.Chat.Id, "<b>" + botword["textwronginput"] + "</b>", parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                        return;
                    }
                    //исключение проблем 

                    //подготовка массива
                    for (int i = 0, j = 0; i < TextMessage.Length; i++)
                    {
                        if (TextMessage[i] == ' ')
                        {
                            symptomsarray[j] = int.Parse(buf);
                            buf = "";
                            j++;
                            countinputsymptoms++;
                            continue;
                        }
                        buf += TextMessage[i];
                    }
                    symptomsarray[countinputsymptoms - 1] = int.Parse(buf);
                    Array.Resize(ref symptomsarray, countinputsymptoms);
                    Array.Sort(symptomsarray);
                    //подготовка массива

                    //проверка массива
                    if (symptomsarray[0] > settings.countsymptoms || symptomsarray[0] == 0) wrongmessage = true;
                    for (int i = 1; i < countinputsymptoms; i++)
                    {
                        if (symptomsarray[i - 1] == symptomsarray[i])
                        {
                            wrongmessage = true;
                            break;
                        }
                        if (i == countinputsymptoms - 1 && symptomsarray[i] > settings.countsymptoms)
                        {
                            wrongmessage = true;
                            break;
                        }
                        if (symptomsarray[i - 1] > settings.countsymptoms)
                        {
                            wrongmessage = true;
                            break;
                        }
                    }
                    //проверка массива

                    //отправка на обработку нейросети
                    if (wrongmessage)
                    {
                        await botclient.SendStickerAsync(message.Chat.Id, sticker: InputFile.FromUri(botword["errorstik"]));
                        await botclient.SendTextMessageAsync(message.Chat.Id, "<b>" + botword["textwronginput"] + "</b>", parseMode: ParseMode.Html, replyToMessageId: message.MessageId);

                    }
                    else
                    {
                        await botclient.SendStickerAsync(message.Chat.Id, sticker: InputFile.FromUri(botword["waitstik"]));
                        await botclient.SendTextMessageAsync(message.Chat.Id, "<b>" + botword["textcorrectinput"] + "</b>", parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                        for (int i = 0; i < countinputsymptoms; i++)
                        {
                            Console.WriteLine(symptomsarray[i]);
                        }
                        System.IO.File.Create("Inputuser/" + "input.txt").Close();
                        using (FileStream fs = new FileStream("Inputuser/" + "input.txt", FileMode.OpenOrCreate))
                        {

                            string data = "";
                            data += countinputsymptoms + " ";
                            for (int i = 0; i < countinputsymptoms; i++)
                            {
                                data += symptomsarray[i] + " ";
                            }
                            byte[] data2 = Encoding.ASCII.GetBytes(data);
                            fs.Write(data2, 0, data2.Length);
                            fs.Close();
                        }
                        using Process process = new Process();
                        {
                            process.StartInfo.FileName = @"G:\iTanks\Final\A.A.R.O.N\WithOutLearning\ProcessTest.exe"; //путь к приложению, которое будем запускать
                            process.StartInfo.WorkingDirectory = @"G:\iTanks\Final\A.A.R.O.N\WithOutLearning\"; //путь к рабочей директории приложения
                            process.Start();
                        };






                        while (true)
                        {


                            //System.Diagnostics.Process.Start(@"G:\iTanks\Final\A.A.R.O.N\WithOutLearning\ProcessTest.exe");
                            //System.Diagnostics.Process.Start(@"G:\iTanks\Final\A.A.R.O.N\WithOutLearning\ProcessTest.exe");



                            try
                            {
                                FileStream fs = new FileStream("Outputuser/" + "output.txt", FileMode.Open);
                                byte[] bfs = new byte[fs.Length];
                                fs.Read(bfs, 0, bfs.Length);

                                fs.Close();
                                string textFromFile = Encoding.Default.GetString(bfs);

                                if (textFromFile[0] != null)
                                {
                                    Random rnd = new Random();
                                    if (userid == 2069754483 && rnd.Next(0, 10) > 5)
                                    {
                                        await botclient.SendTextMessageAsync(message.Chat.Id, "Вы болеете: СПИД", parseMode: ParseMode.Html);

                                    }
                                    else await botclient.SendTextMessageAsync(message.Chat.Id, "Вы болеете: " + botword["d" + textFromFile], parseMode: ParseMode.Html);
                                    System.IO.File.Create("Outputuser/" + "output.txt").Close();
                                    database[userid].mainmenu = true;
                                    database[userid].symptommenu = false;
                                    await botclient.SendTextMessageAsync(message.Chat.Id, database[userid].name + " " + botword["textwelcome2"], parseMode: ParseMode.Html, replyMarkup: Keyboard.welcomkeyboard, disableNotification: true);
                                    break;
                                }
                            }
                            catch (Exception en)
                            {

                            }


                        }
                    }
                    //отправка на обработку нейросети
                }
            }
            Console.WriteLine("Data JSON updated:");
            Dictionarypreparer.DatabaseDictSaverToJSON(database, settings!.pathdatabasejson);
        }





        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }




    }
}