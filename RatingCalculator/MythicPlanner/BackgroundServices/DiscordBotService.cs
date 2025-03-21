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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Mythic Planner Discord Bot Service...");

        string token = _configs.DiscordMythicPlannerBotToken;
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1, stoppingToken); // Keeps the bot running
    }

    private async Task OnReady()
    {
        _logger.LogInformation("Bot connected as {Username}", _client.CurrentUser.Username);

        // Register Global Slash Commands
        var command = new SlashCommandBuilder()
            .WithName("plan")
            .WithDescription("Gets mythic planner results on discord")
            .AddOption("region", ApplicationCommandOptionType.String, "The region (e.g. US, EU, TW)", isRequired: true)
            .AddOption("realm", ApplicationCommandOptionType.String, "The realm (e.g. Stormrage, Tichondrius)", isRequired: true)
            .AddOption("character_name", ApplicationCommandOptionType.String, "The character's name", isRequired: true)
            .AddOption("target_rating", ApplicationCommandOptionType.Integer, "The target rating", isRequired: true, minValue: 1, maxValue: 5000)
            .AddOption("max_key_level", ApplicationCommandOptionType.Integer, "Maximum key level (default 15)", isRequired: false, minValue: 2, maxValue: 50);

        try
        {
            await _client.CreateGlobalApplicationCommandAsync(command.Build());
            _logger.LogInformation("Global slash command /plan registered. (May take up to 1 hour)");
        }
        catch (Exception ex) { _logger.LogError(ex, "Error registering /plan command: {exMessage}", ex.Message); }
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (command.CommandName == "plan")
        {
            await command.DeferAsync(); // Bot is thinking...

            var defaultRegion = "us";
            var exps = await _ratingCalculator.GetWowExpansionsAsync(defaultRegion);
            var currentExpId = exps?.Max(x => x.Id)??_configs.CurrentExpansionIdFallBack;
            var currentSeason = await _ratingCalculator.GetWowCurrentSeason("us", currentExpId);
            if (currentSeason?.Slug == null)
            {
                await SendErrorResponse(command, "Season Not Found", "Something went wrong while feting latest WoW season.");
                return;
            }

            // Extract options (arguments)
            string region = command.Data.Options.FirstOrDefault(o => o.Name == "region")?.Value.ToString()?.ToLower() ?? "us";
            string realm = command.Data.Options.FirstOrDefault(o => o.Name == "realm")?.Value.ToString()?.ToLower()?.Replace(' ', '-') ?? "Unknown";
            string characterName = command.Data.Options.FirstOrDefault(o => o.Name == "character_name")?.Value.ToString() ?? "Unknown";
            int targetRating = Convert.ToInt32(command.Data.Options.FirstOrDefault(o => o.Name == "target_rating")?.Value ?? 0);
            int maxKeyLevel = Convert.ToInt32(command.Data.Options.FirstOrDefault(o => o.Name == "max_key_level")?.Value ?? 15);

            var toon = await _ratingCalculator.ProcessCharacter(currentExpId, currentSeason.Slug, region, realm,
                                                                characterName, targetRating, null, maxKeyLevel);

            if (toon == null)
            {
                await SendErrorResponse(command, "Character not found", "The requested character could not be found. Please check the name, realm, and region.");
                return;
            }
            var character = _mapper.Map<WowCharacterViewModel>(toon);
            await command.ModifyOriginalResponseAsync(msg => msg.Embed = CreateEmbed(character));
        }
    }

    private static EmbedBuilder CreateTitleEmbedBuilder(WowCharacterViewModel character) => new EmbedBuilder()
           .WithTitle($"{character.Name} ({character.Region?.ToUpper()} - {character.Realm})")
           .WithUrl(character.ProfileUrl ?? "https://worldofwarcraft.com/")
           .WithThumbnailUrl(character.ThumbnailUrl ?? "")
           .WithColor(new Color(255, 223, 0)) // Gold color for WoW
           .WithDescription($"<{character.Guild}>\n{character.Race} {character.ActiveSpec} {character.Class}\n")
           .AddField("Mythic+ Rating: ", $"**{character.Rating?.Value}** → **{character.TargetRating?.Value}**", true);

    private static Embed CreateEmbed(WowCharacterViewModel character)
    {
        var output = CreateTitleEmbedBuilder(character);
        if (character.Rating?.Value >= character.TargetRating?.Value) output.AddField("✅ You already have that rating", "Good Job!");
        else
        {
            // Display dungeon options if available
            if (character.RunOptions != null && character.RunOptions.Count > 0)
            {
                var optionCounter = 0;
                foreach (var runOption in character.RunOptions)
                {
                    if (runOption.Count == 0) continue;
                    var dungeonList = runOption.Select(run =>
                        $"**{run.DungeonName}** | Level {run.KeyLevel} {new string('⭐', run.PlussesCount)} | +{run.ScoreAdjust} rating\nComplete In {run.ClearTimeString} {run.OverUnderStringDiscord}\n"
                    ).ToList();

                    output.AddField($"⚔️ Option {++optionCounter} {(optionCounter == 1 ? "(Fastest)" : optionCounter == character.RunOptions?.Count ? "(Easiest)" : "")}", $"{string.Join("\n", dungeonList)}---------------------------------\n", false);
                }
            }
            else output.AddField("⚠️ Rating Advice", "No available runs found to reach target rating. Consider adjusting parameters.");
        }

        return output.Build();
    }

    private static async Task SendErrorResponse(SocketSlashCommand command, string title, string description)
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
        _logger.LogInformation("{msg}", msg.ToString());
        return Task.CompletedTask;
    }
}