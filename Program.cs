// See https://aka.ms/new-console-template for more information
using Microsoft.Data.Sqlite;

var app = new App();

public class App{
    private SqliteConnection connection;
    private int giveUserNumberedOptionsAndReturnTheChoice (int[] validOptions) {
        do
        {
            string userInput = Console.ReadLine();
            if(userInput == "exit"){
                break;
            }
            if(!(
                Int32.TryParse(userInput, out int action)
                && validOptions.Contains(action)
            )){
                Console.WriteLine("This is not a valid response, only the following are valid:");
                foreach (int validOption in validOptions){
                    Console.WriteLine(String.Concat("- ", validOption));
                }
            }
            return action;
        } while (true);
        return 0;
    }
    private string validateAndOutputUsername( int attempt = 0){
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            System.Environment.Exit(1);
        }
        Console.WriteLine("Set your username");
        string username = Console.ReadLine();
        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            SELECT id
            FROM user
            WHERE username = $username
        ";
        command.Parameters.AddWithValue("$username", username);
        var reader = command.ExecuteReader();
        if(reader.Read()){
            Console.WriteLine("Username already exists, choose another one.");
            attempt = attempt + 1;
            return validateAndOutputUsername(attempt);
        }
        return username;
    }
    private string validateAndOutputNewPassword(int attempt = 0){
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            System.Environment.Exit(1);
        }
        Console.WriteLine("Set your password");
        string password = Console.ReadLine();
        Console.WriteLine("Repeat the password");
        string passwordConfirmation = Console.ReadLine();
        if(password != passwordConfirmation){
            Console.WriteLine("Passwords don't match");
            attempt = attempt + 1;
            return validateAndOutputNewPassword(attempt);
        }
        return password;
    }

    public App(){
        connection = new SqliteConnection("Data Source=app.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            CREATE TABLE IF NOT EXISTS `user` (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                password TEXT NOT NULL,
                created_at TEXT DEFAULT (current_timestamp),
                modified_at TEXT DEFAULT (current_timestamp),
                UNIQUE(username)
            );
        ";
        command.ExecuteReader();
        Console.WriteLine("Hello,what would you like to do?");
        Console.WriteLine("1) login");
        Console.WriteLine("2) register");
        Console.WriteLine("Pass exit to terminate");
        int[] validOptions = {1, 2};
        int choice = giveUserNumberedOptionsAndReturnTheChoice(
            validOptions
        );
        if(choice == 2){

            string validatedUsername = validateAndOutputUsername();
            string validatedPassword = validateAndOutputNewPassword();
        }
    }
}

