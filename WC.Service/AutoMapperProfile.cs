using AutoMapper;
using WC.DataAccess.Models;
using WC.Models.DTO;

namespace WC.Service
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<GeoLocationRequest, GeoLocation>().ReverseMap();
            CreateMap<CsvUpload, GeoLocation>().ReverseMap();
            CreateMap<RestCountriesResponse, CountryDetail>().ReverseMap();
        }
    }
}
