using Maboroshi.Bot;
using Maboroshi.Config;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Maboroshi;

internal class Program
{
    private static async Task Main()
    {
        AnsiConsole.Write(new FigletText("MABOROSHI").LeftJustified().Color(Color.Aquamarine1));
        
        AnsiConsole.WriteLine("Welcome to Maboroshi!");

        var bot = new MaboroshiBot(
            config: new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
                .Deserialize<BotConfig>(await File.ReadAllTextAsync("config.yml")),
            sendToUser: ((message, file) =>
            {
                AnsiConsole.WriteLine($"\nBot> {message} ( voice(if have): {file} )");
                return Task.CompletedTask;
            }));

        while (true)
        {
            var s = await AnsiConsole.AskAsync<string>("\nYou> ");
            if (s == ".exit")
            {
                break;
            }
            await bot.GetResponse(s!);
        }
        
        bot.Dispose();
    } 
}