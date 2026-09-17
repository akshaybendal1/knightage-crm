using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IDocumentPdfBuilder
{
    byte[] Build(Document document, IReadOnlyList<DocumentLineItem> lineItems, Lead lead, OrganizationDetails organization, byte[]? logoBytes = null);
}
