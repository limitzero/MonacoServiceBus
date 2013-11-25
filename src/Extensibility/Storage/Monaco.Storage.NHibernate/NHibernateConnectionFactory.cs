using NHibernate;

namespace Monaco.Storage.NHibernate
{
    public class NHibernateConnectionFactory
    {
        private readonly ISessionFactory _factory;

        public NHibernateConnectionFactory(ISessionFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Factory method for creating sessions.
        /// </summary>
        /// <returns></returns>
        public ISession GetCurrentSession()
        {
            return  _factory.OpenSession();
        }
    }
}