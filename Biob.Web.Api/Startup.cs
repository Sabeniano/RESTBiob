using Biob.Data.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Biob.Data.Models;
using Biob.Web.Api.Helpers;
using Biob.Services.Data.Repositories;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Biob.Services.Web.PropertyMapping;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Linq;
using AspNetCoreRateLimit;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Biob.Services.Data.DtoModels.MovieDtos;
using Biob.Services.Data.DtoModels.GenreDtos;
using Biob.Services.Data.DtoModels.TicketDtos;
using Biob.Services.Data.DtoModels.HallDtos;
using Biob.Services.Data.DtoModels.SeatDtos;
using Biob.Services.Data.DtoModels.ShowtimeDtos;
using Swashbuckle.AspNetCore.Swagger;

namespace Biob.Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                var xmlSerializerInputFormatter = new XmlSerializerInputFormatter(setupAction);
                setupAction.InputFormatters.Add(xmlSerializerInputFormatter);


                var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();

                if (jsonOutputFormatter != null)
                {
                    jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.biob.json+hateoas");
                }

            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1); ;

            services.AddSwaggerGen(setupAction => 
            {
                setupAction.EnableAnnotations();
                setupAction.SwaggerDoc("v1", new Info { Title = "Biob RESTful api", Version = "v1" });
            });

            services.AddCors();

            //services.AddCors(options => 
            //{
            //    options.AddPolicy("default", policy =>
            //    {
            //        policy.WithOrigins("http://localhost:3000")
            //            .AllowAnyHeader()
            //            .AllowAnyMethod();
            //    });
            //});

            //services.AddAuthorization();

            services.AddAuthentication("Bearer")
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = "https://localhost:44393/";
                options.RequireHttpsMetadata = false;

                options.ApiName = "BiobApi";
            });

            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddIdentityServerAuthentication(options =>
            //    {
            //        //  IP of the identity server
            //        options.Authority = "https://localhost:44393/";
            //        options.RequireHttpsMetadata = false;
            //        options.ApiName = "Biob.Web.Api";
            //    });
                //.AddJwtBearer(options => 
                //{
                //    //  needs to  be configured
                //    options.Authority = "";
                //    options.Audience = "";
                //});

            var connectionString = Configuration.GetConnectionString("BiobDB");
            services.AddDbContext<BiobDataContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IGenreRepository, GenreRepository>();
            services.AddScoped<IMovieGenreRepository, MovieGenreRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IHallRepository, HallRepository>();
            services.AddScoped<ISeatRepository, SeatRepository>();
            services.AddScoped<IShowtimeRepository, ShowtimeRepository>();

            

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddScoped<IUrlHelper, UrlHelper>(implementationFactory =>
            {
                var actionContext = implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                return new UrlHelper(actionContext);
            });

            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<ITypeHelperService, TypeHelperService>();

            services.AddHttpCacheHeaders(
            (expirationOptions) =>
            {
                expirationOptions.MaxAge = 600;

            },
            (validationOptions) =>
            {
                validationOptions.MustRevalidate = true;
                validationOptions.Vary = new string[] { "Accept-Encoding" };
            });

            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>(options => 
            {
                options.GeneralRules = new List<RateLimitRule>()
                {
                    //  can add more rules but this is fine for now
                    new RateLimitRule()
                    {
                        Endpoint = "*",
                        Limit = 1000,
                        Period = "15m"
                    }
                };
            });

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(options =>
            {
                options
                    //  consider changing in produciton
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            //app.UseCors("default");

            app.UseAuthentication();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddFile("Logs/BioB-{Date}.txt");

            Mapper.Initialize(config => 
            {
                config.CreateMap<Movie, MovieDto>()
                .ForMember(dest => dest.Length, opt => opt.MapFrom(src => src.LengthInSeconds.CalculateFromSeconds()))
                .ForMember(dest => dest.Genre, opt => opt.MapFrom(src => src.MovieGenres.Select(moviegenre => moviegenre.Genre.GenreName).ConvertIEnumerableToString()));

                config.CreateMap<MovieToCreateDto, Movie>();
                config.CreateMap<MovieToUpdateDto, Movie>();
                config.CreateMap<Movie, MovieToUpdateDto>();

                config.CreateMap<Genre, GenreDto>();
                config.CreateMap<GenreToCreateDto, Genre>();
                config.CreateMap<GenreToUpdateDto, Genre>();
                config.CreateMap<Genre, GenreToUpdateDto>();

                config.CreateMap<Ticket, TicketDto>();
                config.CreateMap<TicketToCreateDto, Ticket>();
                config.CreateMap<TicketToUpdateDto, Ticket>();
                config.CreateMap<Ticket, TicketToUpdateDto>();
              
                config.CreateMap<Hall, HallDto>();
                config.CreateMap<HallToCreateDto, Hall>();
                config.CreateMap<HallToUpdateDto, Hall>();
                config.CreateMap<Hall, HallToUpdateDto>();

                config.CreateMap<Seat, SeatDto>();
                config.CreateMap<SeatToCreateDto, Seat>();
                config.CreateMap<SeatToUpdateDto, Seat>();
                config.CreateMap<Seat, SeatToUpdateDto>();

                config.CreateMap<Showtime, ShowtimeDto>();
                config.CreateMap<ShowtimeToCreateDto, Showtime>();
                config.CreateMap<ShowtimeToUpdateDto, Showtime>();
                config.CreateMap<Showtime, ShowtimeToUpdateDto>();
            });

            app.UseHttpsRedirection();
            app.UseIpRateLimiting();
            app.UseHttpCacheHeaders();
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.EnableDeepLinking();
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Biob RESTFUL api v1");
            });

        }
    }
}
