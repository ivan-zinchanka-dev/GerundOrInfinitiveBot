using Microsoft.EntityFrameworkCore;

namespace GerundOrInfinitiveBot.Models;

[PrimaryKey(nameof(Id))]
public class Example
{
    public int Id { get; private set; }
    public string Sentence { get; set; }
    public string Missing { get; set; }
}