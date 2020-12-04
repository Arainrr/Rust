[Factory("meta")]
public class Meta : ConsoleSystem
{
	[ServerVar(Clientside = true, Help = "add <convar> <amount> - adds amount to convar")]
	public static void add(Arg args)
	{
		string @string = args.GetString(0);
		float @float = args.GetFloat(1, 0.1f);
		Command command = Find(@string);
		float result;
		if (command == null)
		{
			args.ReplyWith("Convar not found: " + (@string ?? "<null>"));
		}
		else if (args.IsClientside && command.Replicated)
		{
			args.ReplyWith("Cannot set replicated convars from the client (use sv to do this)");
		}
		else if (args.IsServerside && command.ServerAdmin && !args.IsAdmin)
		{
			args.ReplyWith("Permission denied");
		}
		else if (!float.TryParse(command.String, out result))
		{
			args.ReplyWith("Convar value cannot be parsed as a number");
		}
		else
		{
			command.Set(result + @float);
		}
	}

	[ClientVar(Help = "if_true <command> <condition> - runs a command if the condition is true")]
	public static void if_true(Arg args)
	{
		string @string = args.GetString(0);
		bool @bool = args.GetBool(1);
		if (@bool)
		{
			ConsoleSystem.Run(Option.Client, @string, @bool);
		}
	}

	[ClientVar(Help = "if_false <command> <condition> - runs a command if the condition is false")]
	public static void if_false(Arg args)
	{
		string @string = args.GetString(0);
		bool @bool = args.GetBool(1, true);
		if (!@bool)
		{
			ConsoleSystem.Run(Option.Client, @string, @bool);
		}
	}

	private static Command Find(string name)
	{
		Command command = Index.Server.Find(name);
		if (command != null)
		{
			return command;
		}
		return null;
	}
}
