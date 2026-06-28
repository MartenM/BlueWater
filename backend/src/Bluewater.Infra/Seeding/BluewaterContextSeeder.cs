using Bogus;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Agenda;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Exams;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.News;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bluewater.Infra.Context;

public class BluewaterContextSeeder
{
    private static readonly string SeedImagesPath = Path.Combine(AppContext.BaseDirectory, "Seeding", "Data", "Images");
    private const int FakerSeed = 42;

    public BluewaterContextSeeder(
        BluewaterContext context,
        UserManager<BlueUser> userManager,
        IFileStorageService fileStorageService,
        ILogger<BluewaterContextSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    private readonly BluewaterContext _context;
    private readonly UserManager<BlueUser> _userManager;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<BluewaterContextSeeder> _logger;

    public async Task SeedAsync()
    {
        if (_context.Users.Any())
        {
            _logger.LogInformation("No database seeding. Users already exists.");
            return;
        }

        _logger.LogInformation("Seeding database...");

        var faker = new Faker("nl") { Random = new Randomizer(FakerSeed) };

        var adminUser = await CreateAdminUserAsync();
        var memberUsers = await CreateMemberUsersAsync(faker, count: 20);

        var season = CreateSeason();
        var examTypes = CreateExamTypes();
        var manufacturers = CreateManufacturers();
        var equipmentTypes = CreateEquipmentTypes();
        var oarSets = CreateOarSets(manufacturers);
        CreateEquipment(equipmentTypes, manufacturers, oarSets, examTypes);

        var (generalCategory, trainingCategory) = CreateGroupCategories();
        var (membersGroup, maintainersGroup, seniorsGroup, juniorsGroup) = CreateGroups(generalCategory, trainingCategory);
        CreateGroupInstances(season, adminUser, memberUsers, membersGroup, maintainersGroup, seniorsGroup, juniorsGroup, faker);

        CreateUserExams(faker, memberUsers, examTypes);
        CreateMemberClusters(generalCategory, trainingCategory, membersGroup, seniorsGroup, juniorsGroup);
        await CreateNewsAsync();
        CreateAgendaItems();

        await _context.SaveChangesAsync();
    }

    private async Task<BlueUser> CreateAdminUserAsync()
    {
        var user = new BlueUser
        {
            Email = "admin@example.com",
            UserName = "admin",
            EmailConfirmed = true,
            Firstname = "Admin",
            SurnamePrefix = "der",
            Surname = "Localhost",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = BlueUserSex.Unknown,
            PhoneNumber = "+31612345678",
        };
        VerifyResult(await _userManager.CreateAsync(user, "admin"));

        var saved = await _userManager.FindByNameAsync("admin") ?? throw new Exception("Admin user not found");

        using var stream = new MemoryStream(LoadSeedImage("example-user-profile.png"));
        var stored = await _fileStorageService.StoreAsync(stream, "example-user-profile.png", "image/png");
        saved.ProfilePictureFileId = stored.Id;

        return saved;
    }

    private async Task<List<BlueUser>> CreateMemberUsersAsync(Faker faker, int count)
    {
        var userFaker = new Faker<BlueUser>("nl")
            .UseSeed(FakerSeed)
            .RuleFor(u => u.Firstname, f => f.Name.FirstName())
            .RuleFor(u => u.SurnamePrefix, f => f.PickRandom("", "", "", "van", "de", "van de", "van den"))
            .RuleFor(u => u.Surname, f => f.Name.LastName())
            .RuleFor(u => u.EmailConfirmed, _ => true)
            .RuleFor(u => u.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Between(
                new DateTime(1975, 1, 1), new DateTime(2007, 12, 31))))
            .RuleFor(u => u.Gender, f => f.PickRandom<BlueUserSex>())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("+316########"))
            .RuleFor(u => u.Address, f => new BlueAddress
            {
                Address = f.Address.StreetAddress(),
                City = f.Address.City(),
                Zip = f.Address.ZipCode("#### ??"),
            });

        var users = new List<BlueUser>();
        foreach (var template in userFaker.Generate(count))
        {
            var username = $"{template.Firstname.ToLower()}.{template.Surname.ToLower()}".Replace(" ", "") + $"-{faker.Random.Int(1000, 9999)}";
            template.UserName = username;
            template.Email = $"{username}@example.com";

            VerifyResult(await _userManager.CreateAsync(template, "Password1!"));
            var saved = await _userManager.FindByNameAsync(username) ?? throw new Exception($"User {username} not found after creation");
            users.Add(saved);
        }

        return users;
    }

    private BlueSeason CreateSeason()
    {
        var year = DateTime.Now.Year;
        var season = _context.Seasons.Add(new BlueSeason
        {
            StartDate = new DateOnly(year, 6, 1),
            EndDate = new DateOnly(year + 1, 5, 31),
        }).Entity;

        _context.AppSettings.Add(new BlueAppSettings { CurrentSeasonId = season.Id });

        return season;
    }

    private List<ExamType> CreateExamTypes()
    {
        var types = new[]
        {
            new ExamType { Id = Guid.NewGuid(), Name = "Basisopleiding", Description = "Introductie tot roeien: techniek, veiligheid en clubregels." },
            new ExamType { Id = Guid.NewGuid(), Name = "Scullen", Description = "Basis sculltechniek voor het roeien in een skiff of dubbel-twee." },
            new ExamType { Id = Guid.NewGuid(), Name = "Stuurman", Description = "Vaartechniek en verantwoordelijkheden als stuurman bij een gesteurd vaartuig." },
            new ExamType { Id = Guid.NewGuid(), Name = "Veiligheid op het water", Description = "Capsize-training, reddingsprocedures en EHBO op het water." },
        };

        _context.ExamTypes.AddRange(types);
        return types.ToList();
    }

    private List<Manufacturer> CreateManufacturers()
    {
        var list = new[]
        {
            new Manufacturer { Id = Guid.NewGuid(), Name = "Empacher" },
            new Manufacturer { Id = Guid.NewGuid(), Name = "Filippi" },
            new Manufacturer { Id = Guid.NewGuid(), Name = "WinTech Racing" },
            new Manufacturer { Id = Guid.NewGuid(), Name = "Concept2" },
            new Manufacturer { Id = Guid.NewGuid(), Name = "Croker" },
        };

        _context.Manufacturers.AddRange(list);
        return list.ToList();
    }

    private List<EquipmentType> CreateEquipmentTypes()
    {
        var list = new[]
        {
            new EquipmentType { Id = Guid.NewGuid(), Code = "1x",  Name = "Skiff",              Scull = true,  Coxed = false, RowersCount = 1, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "2x",  Name = "Dubbel-twee",        Scull = true,  Coxed = false, RowersCount = 2, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "2-",  Name = "Twee zonder",        Scull = false, Coxed = false, RowersCount = 2, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "4x",  Name = "Vier dubbel",        Scull = true,  Coxed = false, RowersCount = 4, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "4-",  Name = "Vier zonder",        Scull = false, Coxed = false, RowersCount = 4, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "4+",  Name = "Vier met stuurman",  Scull = false, Coxed = true,  RowersCount = 4, IsBoat = true },
            new EquipmentType { Id = Guid.NewGuid(), Code = "8+",  Name = "Acht",               Scull = false, Coxed = true,  RowersCount = 8, IsBoat = true },
        };

        _context.EquipmentTypes.AddRange(list);
        return list.ToList();
    }

    private List<OarSet> CreateOarSets(List<Manufacturer> manufacturers)
    {
        var concept2 = manufacturers.Single(m => m.Name == "Concept2");
        var croker = manufacturers.Single(m => m.Name == "Croker");

        var list = new[]
        {
            new OarSet { Id = Guid.NewGuid(), Name = "Concept2 Smoothie 2 Sculls",   ManufacturerId = concept2.Id, Scull = true  },
            new OarSet { Id = Guid.NewGuid(), Name = "Concept2 Big Blade Sweeps",     ManufacturerId = concept2.Id, Scull = false },
            new OarSet { Id = Guid.NewGuid(), Name = "Croker Dreher Sculls",          ManufacturerId = croker.Id,   Scull = true  },
            new OarSet { Id = Guid.NewGuid(), Name = "Croker Silver Arrow Sweeps",    ManufacturerId = croker.Id,   Scull = false },
        };

        _context.OarSets.AddRange(list);
        return list.ToList();
    }

    private void CreateEquipment(
        List<EquipmentType> types,
        List<Manufacturer> manufacturers,
        List<OarSet> oarSets,
        List<ExamType> examTypes)
    {
        var empacher  = manufacturers.Single(m => m.Name == "Empacher");
        var filippi   = manufacturers.Single(m => m.Name == "Filippi");
        var wintech   = manufacturers.Single(m => m.Name == "WinTech Racing");

        var type8     = types.Single(t => t.Code == "8+");
        var type4x    = types.Single(t => t.Code == "4x");
        var type4min  = types.Single(t => t.Code == "4-");
        var type4plus = types.Single(t => t.Code == "4+");
        var type2x    = types.Single(t => t.Code == "2x");
        var type1x    = types.Single(t => t.Code == "1x");

        var scullOars = oarSets.First(o => o.Scull);
        var sweepOars = oarSets.First(o => !o.Scull);

        var scullenExam = examTypes.Single(e => e.Name == "Scullen");
        var stuurmanExam = examTypes.Single(e => e.Name == "Stuurman");

        var boats = new[]
        {
            new Equipment { Id = Guid.NewGuid(), Name = "De Sterke",      EquipmentTypeId = type8.Id,    ManufacturerId = empacher.Id, OarSetId = sweepOars.Id, RequiredExamTypeId = stuurmanExam.Id, RowersWeight = 700, RowersWeightMax = 840, DateBuild = new DateOnly(2018, 3, 1),  DateBought = new DateOnly(2018, 6, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "De Vlugge",      EquipmentTypeId = type8.Id,    ManufacturerId = filippi.Id,  OarSetId = sweepOars.Id, RequiredExamTypeId = stuurmanExam.Id, RowersWeight = 700, RowersWeightMax = 840, DateBuild = new DateOnly(2021, 2, 1),  DateBought = new DateOnly(2021, 5, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Maas",           EquipmentTypeId = type4x.Id,   ManufacturerId = empacher.Id, OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  RowersWeight = 320, RowersWeightMax = 380, DateBuild = new DateOnly(2019, 4, 1),  DateBought = new DateOnly(2019, 7, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Rijn",           EquipmentTypeId = type4x.Id,   ManufacturerId = wintech.Id,  OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  RowersWeight = 320, RowersWeightMax = 380, DateBuild = new DateOnly(2020, 3, 1),  DateBought = new DateOnly(2020, 6, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Ijssel",         EquipmentTypeId = type4min.Id,  ManufacturerId = filippi.Id,  OarSetId = sweepOars.Id, RowersWeight = 280, RowersWeightMax = 340, DateBuild = new DateOnly(2017, 5, 1),  DateBought = new DateOnly(2017, 8, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Vecht",          EquipmentTypeId = type4plus.Id, ManufacturerId = wintech.Id,  OarSetId = sweepOars.Id, RequiredExamTypeId = stuurmanExam.Id, RowersWeight = 280, RowersWeightMax = 360, DateBuild = new DateOnly(2016, 6, 1),  DateBought = new DateOnly(2016, 9, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Alpha",          EquipmentTypeId = type2x.Id,   ManufacturerId = empacher.Id, OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  RowersWeight = 140, RowersWeightMax = 180, DateBuild = new DateOnly(2022, 1, 1),  DateBought = new DateOnly(2022, 4, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Beta",           EquipmentTypeId = type2x.Id,   ManufacturerId = empacher.Id, OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  RowersWeight = 140, RowersWeightMax = 180, DateBuild = new DateOnly(2022, 1, 1),  DateBought = new DateOnly(2022, 4, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Gamma",          EquipmentTypeId = type2x.Id,   ManufacturerId = wintech.Id,  OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  FreeFleet = true, DateBuild = new DateOnly(2015, 9, 1), DateBought = new DateOnly(2016, 1, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Delta",          EquipmentTypeId = type1x.Id,   ManufacturerId = empacher.Id, OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  FreeFleet = true, DateBuild = new DateOnly(2023, 2, 1), DateBought = new DateOnly(2023, 5, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Epsilon",        EquipmentTypeId = type1x.Id,   ManufacturerId = filippi.Id,  OarSetId = scullOars.Id, RequiredExamTypeId = scullenExam.Id,  FreeFleet = true, DateBuild = new DateOnly(2020, 7, 1), DateBought = new DateOnly(2020, 10, 1) },
            new Equipment { Id = Guid.NewGuid(), Name = "Zeta", EquipmentTypeId = type1x.Id, ManufacturerId = wintech.Id, OarSetId = scullOars.Id, OutOfOrder = true, DateBuild = new DateOnly(2010, 1, 1), DateBought = new DateOnly(2010, 4, 1), DateSold = new DateOnly(2024, 1, 1) },
        };

        _context.Equipment.AddRange(boats);
    }

    private (UserGroupCategory general, UserGroupCategory training) CreateGroupCategories()
    {
        var general = _context.UserGroupCategories.Add(new UserGroupCategory
        {
            Name = "Algemeen",
            Description = "Algemene ledengroepen.",
            Roles = new List<UserGroupCategoryRole>
            {
                new() { Id = Guid.NewGuid(), SortOrder = 1, NamePlural = "Leden",          NameMasculine = "Lid",          NameFeminine = "Lid" },
                new() { Id = Guid.NewGuid(), SortOrder = 2, NamePlural = "Bestuursleden",   NameMasculine = "Bestuurslid",  NameFeminine = "Bestuurslid" },
            },
        }).Entity;

        var training = _context.UserGroupCategories.Add(new UserGroupCategory
        {
            Name = "Training",
            Description = "Trainingsgroepen per niveau.",
            Roles = new List<UserGroupCategoryRole>
            {
                new() { Id = Guid.NewGuid(), SortOrder = 1, NamePlural = "Sporters",  NameMasculine = "Sporter",  NameFeminine = "Sportster" },
                new() { Id = Guid.NewGuid(), SortOrder = 2, NamePlural = "Coaches",   NameMasculine = "Coach",    NameFeminine = "Coach" },
                new() { Id = Guid.NewGuid(), SortOrder = 3, NamePlural = "Trainers",  NameMasculine = "Trainer",  NameFeminine = "Trainster" },
            },
        }).Entity;

        return (general, training);
    }

    private (UserGroup members, UserGroup maintainers, UserGroup seniors, UserGroup juniors) CreateGroups(
        UserGroupCategory general, UserGroupCategory training)
    {
        var members = _context.UserGroups.Add(new UserGroup
        {
            Name = "Leden",
            Description = "Actieve leden van de vereniging.",
            UserGroupCategoryId = general.Id,
        }).Entity;

        var maintainers = _context.UserGroups.Add(new UserGroup
        {
            Name = "Beheerders",
            Description = "Beheerders met toegang tot alle sitefunctionaliteiten.",
            UserGroupCategoryId = general.Id,
            Permissions = Enum.GetValues<BluePermission>()
                .Select(p => new UserGroupPermission { Id = Guid.NewGuid(), Permission = p })
                .ToList(),
        }).Entity;

        var seniors = _context.UserGroups.Add(new UserGroup
        {
            Name = "Senioren",
            Description = "Seniorentrainingsgroep.",
            UserGroupCategoryId = training.Id,
        }).Entity;

        var juniors = _context.UserGroups.Add(new UserGroup
        {
            Name = "Junioren",
            Description = "Juniorentrainingsgroep (t/m 18 jaar).",
            UserGroupCategoryId = training.Id,
        }).Entity;

        return (members, maintainers, seniors, juniors);
    }

    private void CreateGroupInstances(
        BlueSeason season,
        BlueUser adminUser,
        List<BlueUser> memberUsers,
        UserGroup membersGroup,
        UserGroup maintainersGroup,
        UserGroup seniorsGroup,
        UserGroup juniorsGroup,
        Faker faker)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var allMemberIds = memberUsers.Select(u => u.Id).ToList();
        var cutoff = today.AddYears(-18);
        var seniorIds = memberUsers.Where(u => u.DateOfBirth <= cutoff).Select(u => u.Id).ToList();
        var juniorIds = memberUsers.Where(u => u.DateOfBirth > cutoff).Select(u => u.Id).ToList();

        _context.UserGroupInstances.Add(new UserGroupInstance
        {
            UserGroupId = membersGroup.Id,
            SeasonId = season.Id,
            Members = allMemberIds
                .Concat([adminUser.Id])
                .Select(id => new UserGroupInstanceMember { UserId = id })
                .ToList(),
        });

        _context.UserGroupInstances.Add(new UserGroupInstance
        {
            UserGroupId = maintainersGroup.Id,
            SeasonId = season.Id,
            Members = [new UserGroupInstanceMember { UserId = adminUser.Id }],
        });

        _context.UserGroupInstances.Add(new UserGroupInstance
        {
            UserGroupId = seniorsGroup.Id,
            SeasonId = season.Id,
            Members = seniorIds.Select(id => new UserGroupInstanceMember { UserId = id }).ToList(),
        });

        if (juniorIds.Count > 0)
        {
            _context.UserGroupInstances.Add(new UserGroupInstance
            {
                UserGroupId = juniorsGroup.Id,
                SeasonId = season.Id,
                Members = juniorIds.Select(id => new UserGroupInstanceMember { UserId = id }).ToList(),
            });
        }
    }

    private void CreateUserExams(Faker faker, List<BlueUser> users, List<ExamType> examTypes)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var basisExam = examTypes.Single(e => e.Name == "Basisopleiding");
        var scullenExam = examTypes.Single(e => e.Name == "Scullen");
        var stuurmanExam = examTypes.Single(e => e.Name == "Stuurman");
        var veiligheid = examTypes.Single(e => e.Name == "Veiligheid op het water");

        var exams = new List<UserExam>();

        foreach (var user in users)
        {
            // Everyone has the basis exam
            exams.Add(new UserExam
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExamTypeId = basisExam.Id,
                ObtainedAt = today.AddDays(-faker.Random.Int(180, 1000)),
            });

            // ~70% have scullen
            if (faker.Random.Bool(0.7f))
            {
                exams.Add(new UserExam
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ExamTypeId = scullenExam.Id,
                    ObtainedAt = today.AddDays(-faker.Random.Int(60, 800)),
                });
            }

            // ~40% have stuurman
            if (faker.Random.Bool(0.4f))
            {
                exams.Add(new UserExam
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ExamTypeId = stuurmanExam.Id,
                    ObtainedAt = today.AddDays(-faker.Random.Int(30, 600)),
                });
            }

            // ~50% have veiligheid
            if (faker.Random.Bool(0.5f))
            {
                exams.Add(new UserExam
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ExamTypeId = veiligheid.Id,
                    ObtainedAt = today.AddDays(-faker.Random.Int(30, 700)),
                });
            }
        }

        _context.UserExams.AddRange(exams);
    }

    private void CreateMemberClusters(
        UserGroupCategory generalCategory,
        UserGroupCategory trainingCategory,
        UserGroup membersGroup,
        UserGroup seniorsGroup,
        UserGroup juniorsGroup)
    {
        _context.MemberClusters.AddRange(
            new MemberCluster
            {
                Id = Guid.NewGuid(),
                Name = "Alle leden",
                Description = "Alle actieve leden van de vereniging.",
                Criteria =
                [
                    new MemberClusterCriterion
                    {
                        Id = Guid.NewGuid(),
                        Type = ClusterCriterionType.Group,
                        UserGroupId = membersGroup.Id,
                    },
                ],
            },
            new MemberCluster
            {
                Id = Guid.NewGuid(),
                Name = "Senioren",
                Description = "Seniorentrainingsgroep.",
                Criteria =
                [
                    new MemberClusterCriterion
                    {
                        Id = Guid.NewGuid(),
                        Type = ClusterCriterionType.Group,
                        UserGroupId = seniorsGroup.Id,
                    },
                ],
            },
            new MemberCluster
            {
                Id = Guid.NewGuid(),
                Name = "Junioren",
                Description = "Juniorentrainingsgroep.",
                Criteria =
                [
                    new MemberClusterCriterion
                    {
                        Id = Guid.NewGuid(),
                        Type = ClusterCriterionType.Group,
                        UserGroupId = juniorsGroup.Id,
                    },
                ],
            },
            new MemberCluster
            {
                Id = Guid.NewGuid(),
                Name = "Training (alle groepen)",
                Description = "Alle leden die deelnemen aan een trainingsgroep.",
                Criteria =
                [
                    new MemberClusterCriterion
                    {
                        Id = Guid.NewGuid(),
                        Type = ClusterCriterionType.GroupCategory,
                        UserGroupCategoryId = trainingCategory.Id,
                    },
                ],
            }
        );
    }

    private async Task CreateNewsAsync()
    {
        var gyasIcon  = await CreateNewsIconAsync("Gyas", "gyas-logo.png");
        var flagIcon  = await CreateNewsIconAsync("Vlag", "flag.png");
        var bowaIcon  = await CreateNewsIconAsync("Bowa", "bowa.png");

        _context.NewsPosts.AddRange(
            new NewsPost
            {
                Id = Guid.NewGuid(),
                Title = "Open dag: kom roeien bij Gyas",
                ShortText = "Nieuw in Groningen of altijd al willen roeien? Op onze open dag laten we je kennismaken met de boot, de loods en de club. Ervaren roeiers en bestuursleden staan de hele dag klaar om je rond te leiden, vragen te beantwoorden en je een eerste keer mee het water op te nemen.\n\nOok als je nog niet zeker weet of roeien iets voor je is, is dit de perfecte gelegenheid om de sfeer te proeven. We sluiten de dag af met een gezamenlijke borrel in de loods, waar je kennis kunt maken met andere (aspirant-)leden.",
                AdditionalText = "Iedereen is welkom, ervaring is niet nodig. Loop binnen tussen 10:00 en 16:00 uur, dan staat er een boot voor je klaar en is er koffie en thee in de loods.",
                MembersOnly = false,
                IconId = gyasIcon.Id,
            },
            new NewsPost
            {
                Id = Guid.NewGuid(),
                Title = "Vlag uit voor de Varsity",
                ShortText = "Onze eerste acht heeft de Varsity gewonnen! Een prachtige prestatie na een seizoen vol hard trainen, vroege ochtenden op het water en wedstrijden in binnen- en buitenland. De ploeg heeft het hele jaar toegewerkt naar dit moment en dat heeft zich dubbel en dwars uitbetaald.\n\nNamens het bestuur en alle leden: heel veel felicitaties aan de roeiers, de stuurman en de coaches die hier dag in dag uit aan hebben bijgedragen. De vlag hangt vanaf vandaag trots bij de loods.",
                AdditionalText = "De wedstrijd werd verroeid op de Bosbaan, waar onze ploeg een ruime voorsprong nam in de laatste 500 meter. Felicitaties aan iedereen die heeft bijgedragen aan dit resultaat.",
                MembersOnly = false,
                IconId = flagIcon.Id,
            },
            new NewsPost
            {
                Id = Guid.NewGuid(),
                Title = "Nieuwe samenwerking met Bowa",
                ShortText = "Gyas en Bowa slaan de handen ineen voor onderhoud aan de botenloods en materiaal. Bowa brengt jarenlange ervaring mee op het gebied van onderhoud en renovatie, en gaat ons helpen om de loods en onze vloot in topconditie te houden.\n\nDe samenwerking is voor meerdere seizoenen aangegaan en omvat onder andere periodiek onderhoud aan de boten, kleine reparaties en advies bij toekomstige verbouwingen aan de loods.",
                AdditionalText = "Dankzij deze samenwerking kunnen we onze boten en loods het komende seizoen op niveau houden. Meer details volgen tijdens de eerstvolgende ledenvergadering.",
                MembersOnly = false,
                IconId = bowaIcon.Id,
            },
            new NewsPost
            {
                Id = Guid.NewGuid(),
                Title = "Agenda algemene ledenvergadering",
                ShortText = "De agenda voor de komende ALV staat online. Leden kunnen deze inzien in het ledenportaal en worden van harte uitgenodigd om aanwezig te zijn en hun stem te laten horen over de plannen voor het komende seizoen.\n\nMocht je zelf een onderwerp willen agenderen, dan kan dat tot een week voor de vergadering via het bestuur. Inloop is vanaf 19:30 uur met koffie en thee, de vergadering start om 20:00 uur.",
                AdditionalText = "Onderwerpen op de agenda zijn onder andere de begroting voor het nieuwe seizoen en de verkiezing van twee nieuwe bestuursleden.",
                MembersOnly = true,
                IconId = gyasIcon.Id,
            }
        );
    }

    private void CreateAgendaItems()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        _context.AgendaItems.AddRange(
            new AgendaItem
            {
                Id = Guid.NewGuid(),
                Date = today.AddDays(-30),
                Time = new TimeOnly(19, 30),
                Title = "Algemene ledenvergadering (najaar)",
                Description = "De halfjaarlijkse ALV met onder andere de begroting en een terugblik op het afgelopen seizoen.",
                EndTime = new TimeOnly(21, 30),
            },
            new AgendaItem
            {
                Id = Guid.NewGuid(),
                Date = today.AddDays(-10),
                Time = new TimeOnly(9, 0),
                Title = "Onderhoudsochtend botenloods",
                Description = "Samen met Bowa de boten en de loods klaarmaken voor het nieuwe seizoen.",
                EndTime = new TimeOnly(13, 0),
            },
            new AgendaItem
            {
                Id = Guid.NewGuid(),
                Date = today.AddDays(3),
                Time = new TimeOnly(10, 0),
                Title = "Open dag: kom roeien bij Gyas",
                Description = "Kennismaken met de boot, de loods en de club. Ervaren roeiers en bestuursleden staan klaar om je rond te leiden.",
                EndTime = new TimeOnly(16, 0),
            },
            new AgendaItem
            {
                Id = Guid.NewGuid(),
                Date = today.AddDays(7),
                Title = "Clubkampioenschappen",
                Description = "Het hele weekend wedstrijden tussen de eigen ploegen, afgesloten met een feestavond in de loods.",
                EndDate = today.AddDays(8),
            },
            new AgendaItem
            {
                Id = Guid.NewGuid(),
                Date = today.AddDays(21),
                Time = new TimeOnly(19, 30),
                Title = "Algemene ledenvergadering (voorjaar)",
                Description = "Verkiezing van twee nieuwe bestuursleden en een vooruitblik op het komende seizoen.",
                EndTime = new TimeOnly(21, 30),
            }
        );
    }

    private async Task<NewsIcon> CreateNewsIconAsync(string name, string fileName)
    {
        using var content = new MemoryStream(LoadSeedImage(fileName));
        var storedFile = await _fileStorageService.StoreAsync(content, fileName, "image/png");
        var icon = new NewsIcon { Id = Guid.NewGuid(), Name = name, FileId = storedFile.Id };
        _context.NewsIcons.Add(icon);
        return icon;
    }

    private static byte[] LoadSeedImage(string fileName) =>
        File.ReadAllBytes(Path.Combine(SeedImagesPath, fileName));

    private static void VerifyResult(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new Exception(result.Errors.First().Description);
    }
}
