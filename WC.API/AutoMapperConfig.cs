﻿using AutoMapper;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;

namespace WC.API
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<GeoLocationRequest, GeoLocationInfo>().ReverseMap();
            CreateMap<CsvUpload, GeoLocationInfo>().ReverseMap();

            CreateMap<CountryDetail, RestCountries>().ReverseMap();
        }
    }
}
