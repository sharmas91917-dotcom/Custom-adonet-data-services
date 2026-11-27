using System;
using System.Data.Services;
using System.Data.Services.Common;
using System.Linq;
using System.ServiceModel;

namespace CustomDataServiceDemo
{
    public class CustomProductContext
    {
        private readonly ICachedProductRepository _repository;

        public CustomProductContext()
            : this(new CachedAdoProductRepository())
        {
        }

        public CustomProductContext(ICachedProductRepository repository)
        {
            _repository = repository;
        }

        public IQueryable<Product> Products => _repository.GetAllProducts().AsQueryable();
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = false)]
    public class CustomProductDataService : DataService<CustomProductContext>
    {
        private static readonly ICachedProductRepository Repository = new CachedAdoProductRepository();

        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("Products", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
            config.UseVerboseErrors = false;
        }

        [QueryInterceptor("Products")]
        public System.Linq.Expressions.Expression<Func<Product, bool>> OnQueryProducts()
        {
            return p => p.Stock >= 0;
        }

        [ChangeInterceptor("Products")]
        public void OnChangeProducts(Product product, UpdateOperations operations)
        {
            Repository.InvalidateCache();
        }

        [WebGet]
        public IQueryable<Product> GetExpensiveProducts(decimal minPrice)
        {
            return Repository.GetAllProducts()
                             .Where(p => p.Price >= minPrice)
                             .AsQueryable();
        }
    }
}
