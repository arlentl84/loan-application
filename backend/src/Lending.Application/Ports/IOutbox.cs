namespace Lending.Application;

public interface IOutbox
{    
    void Enqueue(ApplicationApproved applicationApproved);
}
