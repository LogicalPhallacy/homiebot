using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace homiebot 
{
    public class DiceCommands : BaseCommandModule
    {
        private const string regex = @"(?:(\d+)\s*X\s*)?(\d*)D(\d*)((?:[+\/*-]\d+)|(?:[-][LH]))?";
        private readonly Random random;
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private Regex roll;
        public DiceCommands(Random random, ILogger<HomieBot> logger, IConfiguration config)
        {
            this.random = random;
            this.logger = logger;
            this.config = config;
            roll = new Regex(regex,RegexOptions.IgnoreCase);
        }

        [Command("Roll")]
        [Description("Rolls dice from a command string with this syntax:\n"+
        "[<NumRolls>X]<NumDice>d<Sides>[<Operator>NumbertoOperateWith][-<L/H>]")]
        public async Task Roll(CommandContext context, string diceroll)
        {
            await context.TriggerTypingAsync();
             var regexmatch = roll.Match(diceroll);
             if(regexmatch.Success)
             {
                 logger.LogInformation("input string {string} is a valid dice roll, parsing math",diceroll);
                 int numrolls = 1;
                 int dicecount = 0;
                 int sides = 0;
                 int opinteger = 0;
                 DiceOperations operation = DiceOperations.None;
                 foreach(Group g in regexmatch.Groups)
                 {
                     if(g.Success)
                     {
                         try
                         {
                            switch (g.Name)
                            {
                                case "1":
                                    numrolls = int.Parse(g.Value);
                                    if(numrolls > 10)
                                    {
                                        throw new ArgumentOutOfRangeException("You only get ten rolls at a time, thems the rules");
                                    }
                                    break;
                                case "2":
                                    dicecount = int.Parse(g.Value);
                                    if(dicecount > 20)
                                    {
                                        throw new ArgumentOutOfRangeException("You only get twenty dice at a time, thems the rules");
                                    }
                                    break;
                                case "3":
                                    sides = int.Parse(g.Value);
                                    if(sides > 1000)
                                    {
                                        throw new ArgumentOutOfRangeException("No one needs more than a d1000 don't run up the Azure bill");
                                    }
                                    break;
                                case "4":
                                    var parse = ParseOperator(g.Value);
                                    operation = parse.Item1;
                                    opinteger = parse.Item2;
                                    break;
                            }
                         }
                         catch(Exception e)
                         {
                             await HandleError(context,e);
                             return;
                         }
                     }
                 }
                 if(numrolls == 0)
                 {
                    await HandleError(context, new ArgumentOutOfRangeException("You can't have 0 rolls, you're getting one"));
                    numrolls = 1;
                 }
                 logger.LogInformation("We've made it through the parsing, lets roll the dice");
                 await foreach(var roll in GetDiceRoll(dicecount,sides,operation,opinteger,numrolls))
                 {
                     await context.RespondAsync(roll);
                 }
             }
             else
             {
                 logger.LogError("Invalid dice string");
                 await context.RespondAsync("Sorry, I didn't understand that input: {diceroll}");
             }
        }

        private Tuple<DiceOperations,int> ParseOperator(string input)
        {
            switch(input[0])
            {
                case '+':
                    return new Tuple<DiceOperations, int>(DiceOperations.Add, int.Parse(input.Replace("+","")));
                case '-':
                switch (input[1])
                    {
                        case 'L':
                        case 'l':
                            return new Tuple<DiceOperations, int>(DiceOperations.DropLowest,0);
                        case 'H':
                        case 'h':
                            return new Tuple<DiceOperations, int>(DiceOperations.DropHighest,0);
                        default:
                            return new Tuple<DiceOperations, int>(DiceOperations.Subtract, int.Parse(input.Replace("-","")));
                    }
                case '*':
                    return new Tuple<DiceOperations, int>(DiceOperations.Multiply, int.Parse(input.Replace("*","")));
                case '/':
                case '\\':
                    return new Tuple<DiceOperations, int>(DiceOperations.Divide, int.Parse(input.Replace("-","")));
                default:
                    return new Tuple<DiceOperations, int>(DiceOperations.None,0);
            }
        }
        private async Task HandleError(CommandContext context, Exception e)
        {
            await context.RespondAsync($"{e.GetType().Name}! Homie don't play that! Dice Roll Failed: {e.Message}");
        }

        private async IAsyncEnumerable<string> GetDiceRoll(int numdice, int dicesides, DiceOperations firstOp = DiceOperations.None, int opinteger = 0, int numrolls = 1)
        {
            logger.LogInformation("Rolling {numdice}d{dicesides} {numrolls} times. Also doing {diceop} with operatorvalue {opinteger}",numdice,dicesides,numrolls,firstOp,opinteger);
            int grandtotal = 0;
            List<string> Retstrings = new List<string>();
            yield return $"Rolling {numdice} d {dicesides} {numrolls} times:";
            List<int> subtotals = new List<int>();
            int currentroll = 0;
            while(currentroll < numrolls)
            {
                int subtotal = 0;
                string outstr = $"Roll {currentroll+1} Results:\n```\n";
                // do roll here
                int[] diceresult = new int[numdice];
                for(int i=0; i<numdice;i++)
                {
                    diceresult[i] = random.Next(dicesides)+1;
                }
                foreach(string s in getDiceRow(diceresult))
                {
                    outstr += s.Trim();
                    outstr += "\n";
                }
                outstr += "```\n";
                
                // do total here
                switch(firstOp)
                {
                    case DiceOperations.Add:
                        outstr += $"Operation: + {opinteger}";
                        subtotal = diceresult.Sum() + opinteger;
                        break;
                    case DiceOperations.Subtract:
                        outstr += $"Operation: - {opinteger}";
                        subtotal = diceresult.Sum() - opinteger;
                        break;
                    case DiceOperations.Multiply:
                        outstr += $"Operation: * {opinteger}";
                        subtotal = diceresult.Sum() * opinteger;
                        break;
                    case DiceOperations.Divide:
                        outstr += $"Operation: / {opinteger}";
                        if(opinteger == 0)
                        {
                            throw new DivideByZeroException("Nice try");
                        }
                        subtotal = diceresult.Sum() / opinteger;
                        break;
                    case DiceOperations.DropHighest:
                        outstr += $"Dropping highest die: {diceresult.Max()}";
                        subtotal = diceresult.Sum() - diceresult.Max();
                        break;
                    case DiceOperations.DropLowest:
                        outstr += $"Dropping lowest die: {diceresult.Min()}";
                        subtotal = diceresult.Sum() - diceresult.Min();
                        break;
                    default:
                        subtotal = diceresult.Sum();
                        break;
                }
                outstr += $"***Roll Total***: {subtotal}";
                yield return outstr;
                subtotals.Add(subtotal);
                currentroll++;
            }
            if (subtotals.Count == 1)
            {
                yield return $"***GRAND TOTAL:*** {subtotals.FirstOrDefault()}";
            }
            else
            {
                string totalstr = $"Total Sums:";
                foreach (int s in subtotals)
                {
                    grandtotal += s;
                    totalstr += $" {s} +";
                }
                yield return (totalstr.Substring(0,totalstr.Length-1) + $"\n***GRAND TOTAL:*** {grandtotal}");
            }
        }

        private string[] getDiceRow(int[] dicerolled)
        {
            string[] outstr = new string[]{"","",""};
            if(dicerolled.Length >= 10)
            {
                logger.LogInformation("{numdice} rolled, skipping graphics");
                foreach(int die in dicerolled)
                {
                    outstr[0] += "--";
                    outstr[1] += $"{die.ToString()} ";
                    outstr[2] += "--";
                }
            }
            else
            {
                foreach(int die in dicerolled)
                {
                    outstr[0] += "╔";
                    outstr[1] += "║";
                    outstr[2] += "╚";
                    foreach(char c in die.ToString().ToCharArray())
                    {
                        outstr[0] += "═";
                        outstr[1] += c;
                        outstr[2] += "═";
                    }
                    outstr[0] += "╗ ";
                    outstr[1] += "║ ";
                    outstr[2] += "╝ ";
                }
                
            }
            return outstr;
        } 
    }
    internal enum DiceOperations 
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        DropLowest,
        DropHighest,
        None
    }
}