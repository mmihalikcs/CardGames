using System;
using CardGames.Domain;
using CardGames.Infrastructure;

Console.WriteLine("CardGames.Presentation starting...");
Console.WriteLine(DomainMarker.Name);
var infra = new InfrastructureService();
Console.WriteLine(infra.GetInfo());
