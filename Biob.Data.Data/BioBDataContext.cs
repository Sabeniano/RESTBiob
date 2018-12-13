using Biob.Data.Common.Models;
using Biob.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Biob.Data.Data
{
    public class BiobDataContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }

        public BiobDataContext(DbContextOptions<BiobDataContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            //  add audit information before saving
            AddAuditInformation();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //  TODO: add many to many relationship keys
            modelBuilder.Entity<MovieGenre>().HasKey(moviegenre => new { moviegenre.MovieId, moviegenre.GenreId });

            // TODO: add constraints
            // add unique constraint for rowno & seatno together, so you can't have two RowNo = 1; SeatNo = 1;
            //modelBuilder.Entity<Hall>().HasIndex(hall => hall.HallNo).IsUnique();

            //This will deactivate "cascade on delete" function on foreign key for Showtime.HallId - this way, deleting a hall will not delete the related showtimes.
            //The reason this was needed, is that Seat.HallId was also doing this, resulting in errors when EF runs update-database, because it doesn't want multiple of those.
            //Another option was to not have the foreign key on showtimes, but i think just removing the cascade on delete for showtimes would make better sense
            modelBuilder.Entity<Showtime>().HasOne(x => x.Hall).WithMany().HasForeignKey(x => x.HallId).OnDelete(DeleteBehavior.Restrict);

            //Todo: Add precision definition to Ticket.price

            //Todo: Add computed column logic to Hall.NoOfSeats

            //  TODO: add seed data
            modelBuilder.Entity<Genre>().HasData(
                new Genre() { Id = Guid.Parse("8068CBF6-C595-4733-9C24-8104E8454B4C"), GenreName = "Horror" },
                new Genre() { Id = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70"), GenreName = "Drama" },
                new Genre() { Id = Guid.Parse("01C66E3E-8C25-4F5C-A2C5-512C79D09AA6"), GenreName = "Comedy" },
                new Genre() { Id = Guid.Parse("976A7A24-1C25-4A7F-97C6-1A019C5C148D"), GenreName = "Romance" },
                new Genre() { Id = Guid.Parse("72163C34-3D32-4A78-9701-1F708053978F"), GenreName = "Action" },
                new Genre() { Id = Guid.Parse("E143EBFF-A0BD-4107-889F-9BFF26EDA916"), GenreName = "Fiction" },
                new Genre() { Id = Guid.Parse("0532F0DF-C92D-4A10-9D1A-8A5935C541A2"), GenreName = "Biography" },
                new Genre() { Id = Guid.Parse("E48AA8E0-3EFC-4ACD-A0D5-88C82551807A"), GenreName = "Fantasy" },
                new Genre() { Id = Guid.Parse("76C2FAB8-7161-49B7-88C6-F3AAF484EA64"), GenreName = "Family" }
                );

            //  TODO: fix dates possibly?
            modelBuilder.Entity<Movie>().HasData(
                // Movie 1
                new Movie()
                {
                    Id = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"),
                    Title = "The Dark Knight",
                    Description = "When the menace known as the Joker emerges from his mysterious past, he wreaks havoc and chaos on the people of Gotham. The Dark Knight must accept one of the greatest psychological and physical tests of his ability to fight injustice.",
                    LengthInSeconds = 9120,
                    Poster = "https://musicimage.xboxlive.com/catalog/video.movie.8D6KGWZL5PRK/image?locale=en-us&mode=crop&purposes=BoxArt&q=90&h=300&w=200&format=jpg",
                    Producer = "Kevin De La Noy",
                    Actors = "Christopher Nolan",
                    Released = new DateTimeOffset(new DateTime(2005, 5, 12, 0, 0, 0)),
                    AgeRestriction = 16,
                },
                // Movie 2
                new Movie()
                {
                    Id = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"),
                    Title = "SKP",
                    Description = "Nightmare on earth where students fight to survive on a daily basis.",
                    LengthInSeconds = 5400,
                    Poster = "https://static-cdn.jtvnw.net/jtv_user_pictures/nightmare-profile_image-20983b16299fa5d8-300x300.png",
                    Producer = "Jan Eg",
                    Actors = "Balen, Jason, Mikkel",
                    Released = new DateTimeOffset(new DateTime(2016, 5, 12, 0, 0, 0)),
                    AgeRestriction = 16,
                },
                // Movie 3
                new Movie()
                {
                    Id = Guid.Parse("DE9B842D-531A-4F17-AD69-0D3E11CB911D"),
                    Title = "Harry Potter and the Sorcerer's Stone",
                    Description = "Albus Dumbledore, Minerva McGonagall, and Rubeus Hagrid, professors of Hogwarts School of Witchcraft and Wizardry, deliver a recently orphaned infant named Harry Potter to his only remaining relatives, the Dursleys.",
                    LengthInSeconds = 9120,
                    Poster = "data:image/jpeg;base64,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",
                    Producer = "David Heyman",
                    Actors = "Daniel Radcliffe, Emma Watson, Rupert Grint",
                    Released = new DateTimeOffset(new DateTime(2001, 11, 23, 0, 0, 0)),
                    ThreeDee = true,
                    AgeRestriction = 11,
                },
                // Movie 4
                new Movie()
                {
                    Id = Guid.Parse("195C5A46-96F9-446B-B4F7-864AB2DC49DE"),
                    Title = "Bohemian Rhapsody",
                    Description = "A chronicle of the years leading up to Queen's legendary appearance at the Live Aid (1985) concert.",
                    LengthInSeconds = 8040,
                    Poster = "https://upload.wikimedia.org/wikipedia/en/thumb/2/2e/Bohemian_Rhapsody_poster.png/220px-Bohemian_Rhapsody_poster.png",
                    Producer = "Bryan Singer",
                    Actors = "Rami Malek, Lucy Boynton, Joseph Mazzello",
                    Released = new DateTimeOffset(new DateTime(2018, 11, 1, 0, 0, 0)),
                    AgeRestriction = 11,
                },
                // Movie 5
                new Movie()
                {
                    Id = Guid.Parse("174FD8D4-F72B-4059-A7EA-05E687026B0D"),
                    Title = "Venom",
                    Description = "When Eddie Brock acquires the powers of a symbiote, he will have to release his alter-ego 'Venom' to save his life.",
                    LengthInSeconds = 6720,
                    Poster = "https://m.media-amazon.com/images/M/MV5BNzAwNzUzNjY4MV5BMl5BanBnXkFtZTgwMTQ5MzM0NjM@._V1_.jpg",
                    Producer = "Ruben Fleischer",
                    Actors = "Tom Hardy, Michelle Williams, Riz Ahmed",
                    Released = new DateTimeOffset(new DateTime(2018, 10, 11, 0, 0, 0)),
                    ThreeDee = true,
                    AgeRestriction = 18,
                },
                // Movie 6
                new Movie()
                {
                    Id = Guid.Parse("163C03B3-A057-426D-AFA3-1A2631A693E2"),
                    Title = "A Star Is Born",
                    Description = "A musician helps a young singer find fame, even as age and alcoholism send his own career into a downward spiral.",
                    LengthInSeconds = 8160,
                    Poster = "https://upload.wikimedia.org/wikipedia/en/thumb/3/39/A_Star_is_Born.png/220px-A_Star_is_Born.png",
                    Producer = "Bradley Cooper",
                    Actors = "Lady Gaga, Bradley Cooper, Sam Elliott",
                    Released = new DateTimeOffset(new DateTime(2018, 12, 6, 0, 0, 0)),
                    AgeRestriction = 16,
                }
                );

            modelBuilder.Entity<MovieGenre>().HasData(
                // Movie 1
                new MovieGenre() { Id = Guid.Parse("5DF8A672-E37A-4EC2-8B6D-D331C84E7F8D"), MovieId = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"), GenreId = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70") },
                new MovieGenre() { Id = Guid.Parse("C136188F-D71C-4575-8258-1AF4DB16DC0F"), MovieId = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"), GenreId = Guid.Parse("72163C34-3D32-4A78-9701-1F708053978F") },
                // Movie 2
                new MovieGenre() { Id = Guid.Parse("BA8E9182-B327-4089-95F9-AD187C414AAD"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("8068CBF6-C595-4733-9C24-8104E8454B4C") },
                new MovieGenre() { Id = Guid.Parse("50D42FAC-3A09-4BBC-BE99-85D66F664104"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70") },
                new MovieGenre() { Id = Guid.Parse("FF036140-B028-4FE6-82D7-00AB55278518"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("01C66E3E-8C25-4F5C-A2C5-512C79D09AA6") },
                new MovieGenre() { Id = Guid.Parse("944AC338-EA45-4133-9EBB-4D0BD2A1DB3B"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("976A7A24-1C25-4A7F-97C6-1A019C5C148D") },
                new MovieGenre() { Id = Guid.Parse("7C3C5A3D-4566-4C04-BAB2-987EB175F3CA"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("72163C34-3D32-4A78-9701-1F708053978F") },
                new MovieGenre() { Id = Guid.Parse("B7364F6C-9205-46CA-B6C9-8EABFD3DB362"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("E143EBFF-A0BD-4107-889F-9BFF26EDA916") },
                new MovieGenre() { Id = Guid.Parse("1E7F851F-BA66-4989-AFC7-0D193B08575E"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("0532F0DF-C92D-4A10-9D1A-8A5935C541A2") },
                new MovieGenre() { Id = Guid.Parse("F128E5DE-CA20-4672-BE7D-E84A3FB3ED60"), MovieId = Guid.Parse("45C0C223-DE18-4E6E-99EA-AED94E7469F1"), GenreId = Guid.Parse("E48AA8E0-3EFC-4ACD-A0D5-88C82551807A") },
                // Movie 3
                new MovieGenre() { Id = Guid.Parse("8FDEAECF-FD6F-4AD5-AD3D-4B0E64380AB5"), MovieId = Guid.Parse("DE9B842D-531A-4F17-AD69-0D3E11CB911D"), GenreId = Guid.Parse("E48AA8E0-3EFC-4ACD-A0D5-88C82551807A") },
                new MovieGenre() { Id = Guid.Parse("FEA24F3A-5853-404E-ADCB-9A35BF9A0EDB"), MovieId = Guid.Parse("DE9B842D-531A-4F17-AD69-0D3E11CB911D"), GenreId = Guid.Parse("76C2FAB8-7161-49B7-88C6-F3AAF484EA64") },
                // Movie 4
                new MovieGenre() { Id = Guid.Parse("7AD0A3A0-8D31-4F50-83A1-36BF60D3AFB5"), MovieId = Guid.Parse("195C5A46-96F9-446B-B4F7-864AB2DC49DE"), GenreId = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70") },
                new MovieGenre() { Id = Guid.Parse("DE438A37-46B0-4EC4-9AB9-C7797E0733FC"), MovieId = Guid.Parse("195C5A46-96F9-446B-B4F7-864AB2DC49DE"), GenreId = Guid.Parse("0532F0DF-C92D-4A10-9D1A-8A5935C541A2") },
                new MovieGenre() { Id = Guid.Parse("677C944F-2498-4D65-B0D5-6276E6FB3261"), MovieId = Guid.Parse("195C5A46-96F9-446B-B4F7-864AB2DC49DE"), GenreId = Guid.Parse("76C2FAB8-7161-49B7-88C6-F3AAF484EA64") },
                // Movie 5
                new MovieGenre() { Id = Guid.Parse("CCEEA45F-FA85-4E1A-810F-4ECC606F91FA"), MovieId = Guid.Parse("174FD8D4-F72B-4059-A7EA-05E687026B0D"), GenreId = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70") },
                new MovieGenre() { Id = Guid.Parse("AE13C433-3272-43A0-80EC-8DECACF70976"), MovieId = Guid.Parse("174FD8D4-F72B-4059-A7EA-05E687026B0D"), GenreId = Guid.Parse("72163C34-3D32-4A78-9701-1F708053978F") },
                new MovieGenre() { Id = Guid.Parse("20565D3D-A307-40C5-A30C-229764D7B5C6"), MovieId = Guid.Parse("174FD8D4-F72B-4059-A7EA-05E687026B0D"), GenreId = Guid.Parse("E143EBFF-A0BD-4107-889F-9BFF26EDA916") },
                // Movie 6
                new MovieGenre() { Id = Guid.Parse("0CC0D877-05A6-4227-8191-A9AED4F67757"), MovieId = Guid.Parse("163C03B3-A057-426D-AFA3-1A2631A693E2"), GenreId = Guid.Parse("6E0A64BC-7A50-4A6C-9125-8CCF6E54BF70") },
                new MovieGenre() { Id = Guid.Parse("7FB799A4-7EE1-4E72-AA66-ED490BFCF682"), MovieId = Guid.Parse("163C03B3-A057-426D-AFA3-1A2631A693E2"), GenreId = Guid.Parse("01C66E3E-8C25-4F5C-A2C5-512C79D09AA6") },
                new MovieGenre() { Id = Guid.Parse("EA23961B-3379-48AC-862D-28756F2593D2"), MovieId = Guid.Parse("163C03B3-A057-426D-AFA3-1A2631A693E2"), GenreId = Guid.Parse("976A7A24-1C25-4A7F-97C6-1A019C5C148D") },
                new MovieGenre() { Id = Guid.Parse("FCC28F44-22DC-47EB-ACFA-CDCF8F6265C6"), MovieId = Guid.Parse("163C03B3-A057-426D-AFA3-1A2631A693E2"), GenreId = Guid.Parse("76C2FAB8-7161-49B7-88C6-F3AAF484EA64") }
                );

            modelBuilder.Entity<Hall>().HasData(
                new Hall() { Id = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"), HallNo = 1, NoOfSeats = 10, ThreeDee = true},
                new Hall() { Id = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"), HallNo = 2, NoOfSeats = 20 },
                new Hall() { Id = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"), HallNo = 3, NoOfSeats = 20 }
                );

            modelBuilder.Entity<Seat>().HasData(
                new Seat() { Id = Guid.Parse("5FD7F7C4-D90F-4D60-8878-067AF214A0DC"), HallId = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"), RowNo = 1, SeatNo = 1 },
                new Seat() { Id = Guid.Parse("603AB124-4BE6-40FD-9A5E-49BB4A5730DB"), HallId = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"), RowNo = 1, SeatNo = 2 },
                new Seat() { Id = Guid.Parse("F9574335-CE2F-48BE-A275-1BB2718DED0A"), HallId = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"), RowNo = 1, SeatNo = 3 },
                new Seat() { Id = Guid.Parse("245E0E3E-AE50-4E80-B506-01436223F4AA"), HallId = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"), RowNo = 2, SeatNo = 2 },

                new Seat() { Id = Guid.Parse("173595A9-917D-4DF9-9A6D-1A5D5B46104E"), HallId = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"), RowNo = 1, SeatNo = 4 },
                new Seat() { Id = Guid.Parse("70FE1293-99C5-43AA-82DF-FD0BEAA8076A"), HallId = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"), RowNo = 1, SeatNo = 5 },
                new Seat() { Id = Guid.Parse("A66E828E-75AF-4EFE-8E2A-225694CE0BB1"), HallId = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"), RowNo = 1, SeatNo = 6 },
                new Seat() { Id = Guid.Parse("10CA7C8F-CA02-4FB5-B53C-90A554345471"), HallId = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"), RowNo = 2, SeatNo = 1 },

                new Seat() { Id = Guid.Parse("9C55195A-669B-4366-81CF-7796F014537D"), HallId = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"), RowNo = 1, SeatNo = 7 },
                new Seat() { Id = Guid.Parse("979FA768-B42A-444C-944E-4295AE64E00D"), HallId = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"), RowNo = 1, SeatNo = 8 },
                new Seat() { Id = Guid.Parse("CB3314DB-47A0-495A-BDDB-FF2C8F17395D"), HallId = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"), RowNo = 1, SeatNo = 9 },
                new Seat() { Id = Guid.Parse("F1A83A75-1770-47B3-B209-40198951D4AF"), HallId = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"), RowNo = 1, SeatNo = 10 }
                
                

                );

            modelBuilder.Entity<Showtime>().HasData(
                new Showtime()
                {
                    Id = Guid.Parse("092CA7C5-AE83-4A52-A38B-CFC7C8E40E9A"),
                    MovieId = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"),
                    HallId = Guid.Parse("7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E"),
                    TimeOfPlaying = new DateTimeOffset(new DateTime(2018, 12, 22, 10, 0, 0))
                },
                new Showtime()
                {
                    Id = Guid.Parse("5E0D5AD3-22B0-4BDC-808C-62B8F50D0796"),
                    MovieId = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"),
                    HallId = Guid.Parse("288AAD6A-F042-4B36-A5AE-F950AEA18B46"),
                    TimeOfPlaying = new DateTimeOffset(new DateTime(2018, 12, 23, 11, 0, 0))
                },
                new Showtime()
                {
                    Id = Guid.Parse("CF3C5F8E-94EE-494A-B0F1-4A48D9D8291F"),
                    MovieId = Guid.Parse("9D90A452-9547-4D04-98ED-7D617E64AE1E"),
                    HallId = Guid.Parse("D90AC9E4-32BA-4B5C-80EA-6EDA60C0131B"),
                    TimeOfPlaying = new DateTimeOffset(new DateTime(2018, 12, 24, 12, 0, 0))
                }
                );

            modelBuilder.Entity<Ticket>().HasData(
                //Showtime 092CA7C5-AE83-4A52-A38B-CFC7C8E40E9A with HallId 7E9A2751-F1C0-4EB6-A7EC-1319C6DAE31E
                new Ticket()
                {
                    Id = Guid.Parse("303814CA-54F0-4FBB-955B-7FFD33B10B9D"),
                    //CustomerId = Guid.Parse("64C986DF-A168-40CB-B5EA-AB2B20069A08"),
                    ShowtimeId = Guid.Parse("092CA7C5-AE83-4A52-A38B-CFC7C8E40E9A"),
                    SeatId = Guid.Parse("5FD7F7C4-D90F-4D60-8878-067AF214A0DC"),
                    Paid = false,
                    Price = 250
                },
                new Ticket()
                {
                    Id = Guid.Parse("CE442AD4-37A4-43F4-9A6D-5F7AB15DF011"),
                    //CustomerId = Guid.Parse("64C986DF-A168-40CB-B5EA-AB2B20069A08"),
                    ShowtimeId = Guid.Parse("092CA7C5-AE83-4A52-A38B-CFC7C8E40E9A"),
                    SeatId = Guid.Parse("603AB124-4BE6-40FD-9A5E-49BB4A5730DB"),
                    Paid = true,
                    Price = 250
                },
                //Showtime 5E0D5AD3-22B0-4BDC-808C-62B8F50D0796 with HallId 288AAD6A-F042-4B36-A5AE-F950AEA18B46
                new Ticket()
                {
                    Id = Guid.Parse("7F36E8E7-B5CD-43EF-A71D-8CFA2355D8AB"),
                    //CustomerId = Guid.Parse("64C986DF-A168-40CB-B5EA-AB2B20069A08"),
                    ShowtimeId = Guid.Parse("5E0D5AD3-22B0-4BDC-808C-62B8F50D0796"),
                    SeatId = Guid.Parse("173595A9-917D-4DF9-9A6D-1A5D5B46104E"),
                    Paid = false,
                    Price = 300
                },
                new Ticket()
                {
                    Id = Guid.Parse("A9AEE74E-C644-4FC3-9A27-946D7C4CD037"),
                    //CustomerId = Guid.Parse("64C986DF-A168-40CB-B5EA-AB2B20069A08"),
                    ShowtimeId = Guid.Parse("5E0D5AD3-22B0-4BDC-808C-62B8F50D0796"),
                    SeatId = Guid.Parse("70FE1293-99C5-43AA-82DF-FD0BEAA8076A"),
                    Paid = false,
                    Price = 300
                }
                );

            //  get all the deleteable entity types
            var deleteableEntityTypes = modelBuilder.Model.GetEntityTypes().Where(x => x.ClrType != null && typeof(IDeleteable).IsAssignableFrom(x.ClrType));

            foreach (var deletedentityType in deleteableEntityTypes)
            {
                //  add indexer on the IsDeleted property on each of them
                modelBuilder.Entity(deletedentityType.ClrType).HasIndex(nameof(IDeleteable.IsDeleted));
            }

        }

        /// <summary>
        /// Adds audit (createdon or modifiedon) information to entities that inherit IAduit
        /// </summary>
        private void AddAuditInformation()
        {
            //  get the changed entries from the changed tracker
            //  where they are of type IAduit
            //  and their state is either added or modified
            var changedEntities = ChangeTracker.Entries()
                .Where(x => x.Entity is IAudit && (x.State == EntityState.Added || x.State == EntityState.Modified));


            foreach (var entry in changedEntities)
            {
                //  type cast the entity in the entry to IAduit
                var entity = (IAudit)entry.Entity;

                //  if the entity state is added and the createdon property on the entity is default
                //  default of thet type which in this case is null
                //  then assign created on, otherwise assign modified on
                if (entry.State == EntityState.Added && entity.CreatedOn == default(DateTimeOffset))
                {
                    entity.CreatedOn = DateTimeOffset.Now;
                }
                else
                {
                    entity.ModifiedOn = DateTimeOffset.Now;
                }
            }
        }
    }
}
