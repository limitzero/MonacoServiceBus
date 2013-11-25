 if exists (select * from dbo.sysobjects where id = object_id(N'Subscriptions') and OBJECTPROPERTY(id, N'IsUserTable') = 1) drop table Subscriptions
     
 if exists (select * from dbo.sysobjects where id = object_id(N'Timeouts') and OBJECTPROPERTY(id, N'IsUserTable') = 1) drop table Timeouts
  
 create table Subscriptions (
       Id UNIQUEIDENTIFIER not null,
       isActive BIT null,
       [endpoint] NVARCHAR(500) null,
       componentName NVARCHAR(500) null,
       messageName NVARCHAR(500) null,
       primary key (Id)
   )
    
create table Timeouts (
    Id UNIQUEIDENTIFIER not null,
    createdOn DATETIME null,
    modifiedOn DATETIME null,
    invocationAt DATETIME null,
    messageName NVARCHAR(500) null,
    instance VARBINARY(MAX) null,
    [endpoint] NVARCHAR(500) null,
    primary key (Id)
)

   /* -- indexes -- */   
    create index idx_subscription_message_and_uri on Subscriptions (uri, messageName)
  
    create index idx_timeout_endpoint on Timeouts ([endpoint])
    
    create nonclustered index idx_timeouts_id on Timeouts(Id) 

    create nonclustered index idx_subscription_id on Subscriptions(Id)
