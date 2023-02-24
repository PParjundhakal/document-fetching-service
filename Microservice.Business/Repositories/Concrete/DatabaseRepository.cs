using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microservice.Database;
using Microservice.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Business.Repositories.Concrete
{
    public class DatabaseRepository<T> : IDatabaseRepository<T> where T : BaseEntity
    {
        private readonly DatabaseContext _dbContext;
        private readonly DbSet<T> _entities;

        public DatabaseRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _entities = dbContext.Set<T>();
        }

        public void Create(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            _entities.Add(entity);
            _dbContext.SaveChanges();
        }

        public IEnumerable<T> Read()
        {
            return _entities.AsEnumerable();
        }

        public T Read(int id)
        {
            if (id == 0)
                throw new ArgumentNullException(nameof(id));

            return _entities.SingleOrDefault(s => s.Id.Equals(id));
        }

        public IEnumerable<T> Read(Func<T, bool> predicate,
             params Expression<Func<T, object>>[] navigationProperties)
        {
            IQueryable<T> dbQuery = _dbContext.Set<T>();

            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<T, object>(navigationProperty);

            return dbQuery.Where(predicate);
        }

        public T ReadOne(Func<T, bool> predicate,
            params Expression<Func<T, object>>[] navigationProperties)
        {
            IQueryable<T> dbQuery = _dbContext.Set<T>();

            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<T, object>(navigationProperty);

            return dbQuery.SingleOrDefault(predicate);
        }

        public int Count()
        {
            return _entities.AsEnumerable().Count();
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            return _entities.Where(predicate).Count();
        }

        public void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            if (id == 0)
                throw new ArgumentNullException(nameof(id));

            T entity = _entities.SingleOrDefault(s => s.Id.Equals(id));

            if (entity == null)
                throw new Exception("Unable to delete record as it does not exist");

            _entities.Remove(entity);
            _dbContext.SaveChanges();
        }
    }
}