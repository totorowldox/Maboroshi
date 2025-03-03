﻿using Maboroshi.Bot;
using Maboroshi.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Maboroshi;

internal class Program
{
    private static async Task Main()
    {
        Console.WriteLine("Welcome to Maboroshi!");

        var bot = new MaboroshiBot(
            config: new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
                .Deserialize<BotConfig>(await File.ReadAllTextAsync("config.yml")),
            sendToUser: ((message, file) =>
            {
                Console.WriteLine($"\nBot> {message} ( voice(if have): {file} )");
                return Task.CompletedTask;
            }));

        while (true)
        {
            Console.Write("\nYou> ");
            var s = Console.ReadLine();
            if (s == ".exit")
            {
                break;
            }
            await bot.GetResponse(s!);
        }
        
        bot.Dispose();
    } 
}