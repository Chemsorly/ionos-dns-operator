using IonosDnsOperator.Entities;
using KubeOps.Operator.Web.Webhooks.Admission.Validation;

namespace IonosDnsOperator.Webhooks;

[ValidationWebhook(typeof(IonosDnsRecord))]
public class IonosDnsRecordValidationWebhook : ValidationWebhook<IonosDnsRecord>
{
	public override ValidationResult Create(IonosDnsRecord entity, bool dryRun)
	{
		return Success();
	}

	public override ValidationResult Update(IonosDnsRecord oldEntity, IonosDnsRecord newEntity, bool dryRun)
	{
		if (oldEntity.Spec.RootName != newEntity.Spec.RootName)
		{
			return Fail("Spec.RootName is immutable.");
		}
		if (oldEntity.Spec.Name != newEntity.Spec.Name)
		{
			return Fail("Spec.Name is immutable.");
		}
		if (oldEntity.Spec.Type != newEntity.Spec.Type)
		{
			return Fail("Spec.Type is immutable.");
		}

		return Success();
	}
}
