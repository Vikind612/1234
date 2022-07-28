﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BLLWordProc;
using DALWordProc.EFDbContext;
using DALWordProc.Entities;
using DALWordProc.Repository.Implementations;
using DALWordProc.Repository.Interfaces;
using Microsoft.Extensions.CommandLineUtils;
using Unity;
using Unity.Lifetime;

namespace ConsoleWordProc
{
    //на рефакторинг
    class Program
    {
        /// <summary>
        /// Определение кодировки UTF8 по сигнатуре кодировки в фале.
        /// </summary>
        /// <param name="path">Полный путь и имя файла.</param>
        /// <returns>Подтверждена ли кодировка.</returns>
        public static bool IsUTF8(string path)
        {
            DetectEncodingType detect = new DetectEncodingType();
            DetectEncodingTypeUTF8 detectUTF8 = new DetectEncodingTypeUTF8(detect);           
            return detectUTF8.Check(path);           
        }

        /// <summary>
        /// Настройка команд для идентификации ввода в консоли cmd.exe.
        /// </summary>
        /// <param name="args"> аргументы сонсоли cmd.exe</param>
        /// <returns>Настроенный экземпляр реализации команд консоли cmd.exe</returns>
        public static CommandLineApplication InitCommandLine(string[] args)
        {
            

             CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption createDictionary = commandLineApplication.Option(
             "--create | -c",
             "The command to create a dictionary, you must specify the name of the file with the text. A dictionary will be created from the text.",
             CommandOptionType.SingleValue);

             var updateDictionary = commandLineApplication.Option(
                 "--update | -u",
                 "The command to update the dictionary, you must specify the name of the file with the text." +
                 " New words will be added from the text, and existing words in the dictionary will be updated.",
                 CommandOptionType.SingleValue);

             var deleteDictionary = commandLineApplication.Option(
                 "--delete | -d",
                 "The command to delete the dictionary.",
                 CommandOptionType.NoValue);

             commandLineApplication.HelpOption("-? | -h | --help");


             commandLineApplication.OnExecute(() =>
             {
                 //одновременно должна выполняться только одна команда
                 int numberInsertCommand = 0;//счетчик команд

                 foreach(var command in commandLineApplication.GetOptions())
                 {
                     if (command.HasValue())
                     {
                         numberInsertCommand++;
                     }
                 }                

                 if (numberInsertCommand > 1)//команд больше чем одна
                 {
                     Console.WriteLine("You can specify only one command-line parameter at a time (commands)");
                     Environment.Exit(0);
                     return 0;
                 }

                 
                 DBDictionaryWord dbContext;
                 IGenericRepository<DictionaryWord> repo;
                 ManagerDictionary manager;
                 
                 dbContext = new DBDictionaryWord();
                 repo = new GenericRepository<DictionaryWord>(dbContext);
                 manager = new ManagerDictionary(repo);

                 //обработка команд
                 //команда создания словаря
                 if (createDictionary.HasValue())
                 {
                     
                    if (IsUTF8(createDictionary.Value()))
                    {
                         
                        string text = "";
                        text = File.ReadAllText(createDictionary.Value(), Encoding.Default);
                        manager.CreateDictionary(text);                        
                    }
                    else
                    {
                        Console.WriteLine("Error: The file format is not UTF8.");
                    } 
                 }

                 //команда обновления словаря
                 if (updateDictionary.HasValue())
                 {
                     
                    if (IsUTF8(updateDictionary.Value()))
                    {
                        string text = "";
                        text = File.ReadAllText(updateDictionary.Value(), Encoding.Default);
                        manager.UpdateDictionary(text);
                    }
                    else
                    {
                        Console.WriteLine("Error: The file format is not UTF8.");
                    }                     
                 }

                 //команда удаления словаря
                 if (deleteDictionary.HasValue())
                 {
                         
                        manager.DeleteDictionary();
                         
                     }
                 Environment.Exit(0);
                 return 0;
             });
            return commandLineApplication;
             

             
        }

        static void Main(string[] args)
        {
           /* IUnityContainer container = new UnityContainer();
            container.RegisterType<DetectEncodingType>(new ContainerControlledLifetimeManager());
            container.RegisterType<DetectEncodingTypeUTF8>(new ContainerControlledLifetimeManager());
            container.RegisterType<DBDictionaryWord> (new ContainerControlledLifetimeManager());
            container.RegisterType<IGenericRepository<DictionaryWord>, GenericRepository<DictionaryWord>>();
            container.RegisterType<BaseManagerDictionary, ManagerDictionary>(new ContainerControlledLifetimeManager());*/

            



            if (args.Length != 0)
            {
                //Если есть команды
                try
                {
                    var commandLineApplication = InitCommandLine(args);
                    commandLineApplication.Execute(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Environment.Exit(0);
                    return;
                }
            }

            //без параметров командной строки, при этом оно должно автоматически переходить в режим ввода, стандартный поток ввода – с клавиатуры
            StringBuilder prefix = new StringBuilder();
            ConsoleKeyInfo info ;
            while (true)
            {

                info = Console.ReadKey(true);
                Console.Write(info.KeyChar);
                
                if (info.Key == ConsoleKey.Enter && prefix.Length==0 || info.Key == ConsoleKey.Escape)//выход при нажатии Esc или ввода пустой строки
                {
                Environment.Exit(0);
                break;
                }
                else
                if (info.Key == ConsoleKey.Enter && prefix.Length > 0)//ввели не пустую строку
                {
                    List<DictionaryWord> arrayWords ;
                    try
                    {
                          DBDictionaryWord dbContext = new DBDictionaryWord();
                          IGenericRepository<DictionaryWord> repo = new GenericRepository<DictionaryWord>(dbContext);
                          BaseManagerDictionary manager = new ManagerDictionary(repo);

                      //  var manager=container.Resolve<ManagerDictionary>();
                        arrayWords = manager.FindWords(prefix.ToString()).ToList();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Environment.Exit(0);
                        return;
                    }

                    Console.WriteLine();

                    foreach (var word in arrayWords)
                    {
                        Console.WriteLine("- "+word.Word);
                    }
                    prefix.Clear();
                }
                else
                {
                    prefix.Append(info.KeyChar);//собираем символы ввода не esc и не enter
                }
               
            }
        }
        
    }
}
