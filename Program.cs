// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello,what would you like to do?");
Console.WriteLine("1) login");
Console.WriteLine("2) register");
Console.WriteLine("Pass exit to terminate");
int[] validOptions = {1, 2};
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
} while (true);
