using AutoMapper;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using MythicPlanner.Models;
using RcLibrary.Models.Configurations;
using RcLibrary.Servcies.RatingCalculatorServices;

namespace MythicPlanner.BackgroundServices;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IRcService _ratingCalculator;
    private readonly IMapper _mapper;
    private readonly Settings _configs;
    //private readonly Dictionary<ulong, WowCharacterViewModel> _characterCache = new();
    public DiscordBotService(ILogger<DiscordBotService> logger,
                             IOptions<Settings> configs,
                             IRcService ratingCalculator,
                             IMapper mapper)
    {
        _logger = logger;
        _ratingCalculator = ratingCalculator;
        _mapper = mapper;
        _configs = configs.Value;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });

        _client.Log += LogAsync;
        _client.Ready += OnReady;
        _client.SlashCommandExecuted += SlashCommandHandler;
        // _client.ButtonExecuted += ButtonHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Discord Bot Service...");

        string token = _configs.DiscordMythicPlannerBotToken;
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1, stoppingToken); // Keeps the bot running
    }

    private async Task OnReady()
    {
        _logger.LogInformation($"Bot connected as {_client.CurrentUser.Username}");
        // Register Global Slash Commands
        var command = new SlashCommandBuilder()
            .WithName("plan")
            .WithDescription("Gets mythic planner results on discord")
            .AddOption("region", ApplicationCommandOptionType.String, "The region (e.g., US, EU, TW)", isRequired: true)
            .AddOption("realm", ApplicationCommandOptionType.String, "The realm (e.g., Stormrage, Tichondrius)", isRequired: true)
            .AddOption("character_name", ApplicationCommandOptionType.String, "The character's name", isRequired: true)
            .AddOption("target_rating", ApplicationCommandOptionType.Integer, "The target rating", isRequired: true, minValue: 1, maxValue: 5000)
            .AddOption("max_key_level", ApplicationCommandOptionType.Integer, "Maximum key level (default 15)", isRequired: false, minValue: 2, maxValue: 50)
        //.AddOption("result_per_page", ApplicationCommandOptionType.Integer, "How many options per page (default 3)", isRequired: false, minValue: 1, maxValue: 10)
        ;



        try
        {
             await _client.CreateGlobalApplicationCommandAsync(command.Build());

            //----------------- for testing, enable global instead for live
            //var guild = _client.GetGuild(...);
            //await guild.CreateApplicationCommandAsync(command.Build());
            //-----------------


            _logger.LogInformation("Global slash command /ping registered. (May take up to 1 hour)");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error registering /ping command: {ex.Message}");
        }

    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (command.CommandName == "plan")
        {
            await command.DeferAsync();


            // validate input


            var currentExpId = _configs.CurrentExpansionIdFallBack;
            var currentSeason = await _ratingCalculator.GetWowCurrentSeason("us", currentExpId);
            if (currentSeason?.Slug == null)
            {
                await SendErrorResponse(command, "Season Not Found", "Something went wrong while feting latest WoW season.");
                return;
            }


            // Extract options (arguments)
            string region = command.Data.Options.FirstOrDefault(o => o.Name == "region")?.Value.ToString()?.ToLower() ?? "us";
            string realm = command.Data.Options.FirstOrDefault(o => o.Name == "realm")?.Value.ToString()?.ToLower() ?? "Unknown";
            string characterName = command.Data.Options.FirstOrDefault(o => o.Name == "character_name")?.Value.ToString() ?? "Unknown";
            int targetRating = Convert.ToInt32(command.Data.Options.FirstOrDefault(o => o.Name == "target_rating")?.Value ?? 0);
            int maxKeyLevel = Convert.ToInt32(command.Data.Options.FirstOrDefault(o => o.Name == "max_key_level")?.Value ?? 15);
            int resultPerPage = Convert.ToInt32(command.Data.Options.FirstOrDefault(o => o.Name == "result_per_page")?.Value ?? 3000);


            // await command.RespondAsync(response);

            var toon = await _ratingCalculator.ProcessCharacter(currentExpId,
                currentSeason.Slug, region, realm.Replace(' ', '-'), characterName, targetRating, null, maxKeyLevel);

            if (toon == null)
            {
                await SendErrorResponse(command, "Character not found", "The requested character could not be found. Please check the name, realm, and region.");
                return;
            }
            var character = _mapper.Map<WowCharacterViewModel>(toon);
            // _characterCache[command.User.Id] = character;
            var pages = CreateOptionPages(character, resultPerPage);
            int currentPage = 0;

            var embed = new EmbedBuilder()
            .WithTitle($"{character.Name} ({character.Region?.ToUpper()} - {character.Realm})")
            .WithUrl(character.ProfileUrl ?? "https://raider.io/")
            .WithThumbnailUrl(character.ThumbnailUrl ?? "")
            .WithColor(new Color(255, 223, 0)) // Gold color for WoW
            .WithDescription($"<{character.Guild}>\n{character.Race} {character.ActiveSpec} {character.Class}")
            .AddField("Mythic+ Rating: ", $"**{character.Rating?.Value}** → **{character.TargetRating?.Value}**", true)

            .WithFooter($"Last updated: {character.LastCrawledAt:yyyy-MM-dd HH:mm:ss}");


            await command.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = pages[currentPage];
                //  msg.Components = CreatePaginationButtons(currentPage, pages.Count, command.User.Id, resultPerPage);
            });

        }
    }

    //private MessageComponent CreatePaginationButtons(int currentPage, int totalPages, ulong userId, int resultPerPage)
    //{
    //    var builder = new ComponentBuilder();

    //    if (currentPage > 0)
    //        builder.WithButton("⬅️ Previous", $"prev_page:{currentPage - 1}:{userId}:{resultPerPage}", ButtonStyle.Primary, disabled: false);

    //    if (currentPage < totalPages - 1)
    //        builder.WithButton("Next ➡️", $"next_page:{currentPage + 1}:{userId}:{resultPerPage}", ButtonStyle.Primary, disabled: false);

    //    return builder.Build();
    //}


    //private async Task ButtonHandler(SocketMessageComponent component)
    //{
    //    string[] args = component.Data.CustomId.Split(':'); // Extract button data
    //    string action = args[0];
    //    int newPage = int.Parse(args[1]);
    //    ulong userId = ulong.Parse(args[2]); // Get user ID from button
    //    int resultPerPage = int.Parse(args[3]);

    //    // Retrieve cached character data instead of re-fetching
    //    if (!_characterCache.TryGetValue(userId, out var character))
    //    {
    //        await component.RespondAsync("⚠️ Your session has expired. Please use `/plan` again.", ephemeral: true);
    //        return;
    //    }


    //    var pages = CreateOptionPages(character, resultPerPage);

    //    // Update the message with the new page
    //    await component.UpdateAsync(msg =>
    //    {
    //        msg.Embed = pages[newPage];
    //        msg.Components = CreatePaginationButtons(newPage, pages.Count, userId, resultPerPage);
    //    });
    //}

    private EmbedBuilder CreateTitleEmbedBuilder(WowCharacterViewModel character)
    {
        var embed = new EmbedBuilder()
           .WithTitle($"{character.Name} ({character.Region?.ToUpper()} - {character.Realm})")
           .WithUrl(character.ProfileUrl ?? "https://worldofwarcraft.com/")
           .WithThumbnailUrl(character.ThumbnailUrl ?? "")
           .WithColor(new Color(255, 223, 0)) // Gold color for WoW
           .WithDescription($"<{character.Guild}>\n{character.Race} {character.ActiveSpec} {character.Class}\n")
           .AddField("Mythic+ Rating: ", $"**{character.Rating?.Value}** → **{character.TargetRating?.Value}**", true)
           ;

        return embed;
    }

    private List<Embed> CreateOptionPages(WowCharacterViewModel character, int runsPerPage)
    {
        var pages = new List<EmbedBuilder>();


        if (character.Rating?.Value >= character.TargetRating?.Value)
        {
            var embed = CreateTitleEmbedBuilder(character);
            embed.AddField("✅ You already have that rating", "Good Job!");
            pages.Add(embed);
        }
        else
        {

            // Display dungeon options if available
            if (character.RunOptions != null && character.RunOptions.Count > 0)
            {
                var optionCounter = 0;
                for (int i = 0; i < character.RunOptions!.Count; i += runsPerPage)
                {
                    var runOptions = character.RunOptions.Skip(i).Take(runsPerPage).ToList();
                    var embed = CreateTitleEmbedBuilder(character);
                    //embed.AddField("Dungeon Recommendations", "Complete the following dungeons:");

                    // embed.WithFooter($"Page {pages.Count + 1}/{Math.Ceiling((double)character.RunOptions.Count / runsPerPage)}");
                    foreach (var runOption in runOptions)
                    {
                        if (runOption.Count == 0) continue;



                        var dungeonList = runOption.Select(run =>
                            $"**{run.DungeonName}** | Level {run.KeyLevel} {new string('⭐', run.PlussesCount)} | +{run.ScoreAdjust} rating\nComplete In {run.ClearTimeString} {run.OverUnderStringDiscord}\n"
                        ).ToList();

                        embed.AddField($"⚔️ Option {++optionCounter} {(optionCounter == 1 ? "(Fastest)" : optionCounter == character.RunOptions?.Count ? "(Easiest)" : "")}", $"{string.Join("\n", dungeonList)}---------------------------------\n", false);
                    }

                    pages.Add(embed);
                }
            }
            else
            {
                var embed = CreateTitleEmbedBuilder(character);
                embed.AddField("⚠️ Rating Advice", "No available runs found to reach target rating. Consider adjusting parameters.");
                pages.Add(embed);
            }
        }

        return pages.Select(e => e.Build()).ToList();
    }

    private async Task SendErrorResponse(SocketSlashCommand command, string title, string description)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"❌ {title}")
            .WithDescription(description)
            .WithColor(new Color(255, 0, 0)) // Red color for error messages
            .WithFooter("Please check your input and try again.");

        await command.ModifyOriginalResponseAsync(msg => msg.Embed = embed.Build());
    }

    private Task LogAsync(LogMessage msg)
    {
        _logger.LogInformation(msg.ToString());
        return Task.CompletedTask;
    }
}
