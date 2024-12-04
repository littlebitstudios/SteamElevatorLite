if (args.Length > 0) // Specific commands
{
    if (args[0] == "trigger")
    {
        HttpClient client = new HttpClient();
        if (args.Length > 1 && args[1] == "--noconfirm")
        {
            client.GetAsync("http://localhost:12345/trigger?showConfirmation=false").Wait();
        }
        else
        {
            client.GetAsync("http://localhost:12345/trigger").Wait();
        }
    }
    else if (args[0] == "quit")
    {
        HttpClient client = new HttpClient();
        client.GetAsync("http://localhost:12345/quit").Wait();
    }
    else
    {
        Console.WriteLine("Invalid argument.");
    }
}
else // Trigger instantly on no arguments
{
    HttpClient client = new HttpClient();
    client.GetAsync("http://localhost:12345/trigger").Wait();
}