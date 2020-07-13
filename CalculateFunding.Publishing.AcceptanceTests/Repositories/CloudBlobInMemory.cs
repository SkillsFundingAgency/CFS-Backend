using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class CloudBlobInMemory : ICloudBlob
    {
        private string _blobName;
        private IDictionary<string, string> _metadata;

        public string Name => _blobName;

        public CloudBlobClient ServiceClient => throw new NotImplementedException();

        public int StreamWriteSizeInBytes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int StreamMinimumReadSizeInBytes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public BlobProperties Properties => throw new NotImplementedException();

        public IDictionary<string, string> Metadata => _metadata;

        public DateTimeOffset? SnapshotTime => throw new NotImplementedException();

        public bool IsSnapshot => throw new NotImplementedException();

        public Uri SnapshotQualifiedUri => throw new NotImplementedException();

        public StorageUri SnapshotQualifiedStorageUri => throw new NotImplementedException();

        public CopyState CopyState => throw new NotImplementedException();

        public BlobType BlobType => throw new NotImplementedException();

        public Uri Uri => throw new NotImplementedException();

        public StorageUri StorageUri => throw new NotImplementedException();

        public CloudBlobDirectory Parent => throw new NotImplementedException();

        public CloudBlobContainer Container => throw new NotImplementedException();

        public CloudBlobInMemory(string blobName)
        {
            _blobName = blobName;

            _metadata = new Dictionary<string, string>();
        }

        public void AbortCopy(string copyId, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task AbortCopyAsync(string copyId)
        {
            throw new NotImplementedException();
        }

        public Task AbortCopyAsync(string copyId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task AbortCopyAsync(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string AcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginAbortCopy(string copyId, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginAbortCopy(string copyId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginAcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginAcquireLease(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginBreakLease(TimeSpan? breakPeriod, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginBreakLease(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginChangeLease(string proposedLeaseId, AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginChangeLease(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDelete(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDelete(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDeleteIfExists(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToByteArray(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToFile(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToStream(Stream target, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginDownloadToStream(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginExists(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginExists(BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginFetchAttributes(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginFetchAttributes(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginGetAccountProperties(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginGetAccountProperties(BlobRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginOpenRead(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginOpenRead(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginReleaseLease(AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginReleaseLease(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginRenewLease(AccessCondition accessCondition, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginRenewLease(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginSetMetadata(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginSetMetadata(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginSetProperties(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginSetProperties(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromFile(string path, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromFile(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromStream(Stream source, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromStream(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public ICancellableAsyncResult BeginUploadFromStream(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public TimeSpan BreakLease(TimeSpan? breakPeriod = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod)
        {
            throw new NotImplementedException();
        }

        public Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string ChangeLease(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition)
        {
            throw new NotImplementedException();
        }

        public Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Delete(DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool DeleteIfExists(DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteIfExistsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteIfExistsAsync(DeleteSnapshotsOption deleteSnapshotsOption, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public int DownloadRangeToByteArray(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadRangeToByteArrayAsync(byte[] target, int index, long? blobOffset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void DownloadRangeToStream(Stream target, long? offset, long? length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length)
        {
            throw new NotImplementedException();
        }

        public Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task DownloadRangeToStreamAsync(Stream target, long? offset, long? length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public int DownloadToByteArray(byte[] target, int index, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadToByteArrayAsync(byte[] target, int index)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadToByteArrayAsync(byte[] target, int index, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<int> DownloadToByteArrayAsync(byte[] target, int index, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void DownloadToFile(string path, FileMode mode, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToFileAsync(string path, FileMode mode)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToFileAsync(string path, FileMode mode, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToFileAsync(string path, FileMode mode, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void DownloadToStream(Stream target, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToStreamAsync(Stream target)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToStreamAsync(Stream target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task DownloadToStreamAsync(Stream target, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void EndAbortCopy(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public string EndAcquireLease(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public TimeSpan EndBreakLease(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public string EndChangeLease(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndDelete(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public bool EndDeleteIfExists(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public int EndDownloadRangeToByteArray(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndDownloadRangeToStream(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public int EndDownloadToByteArray(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndDownloadToFile(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndDownloadToStream(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public bool EndExists(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndFetchAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public AccountProperties EndGetAccountProperties(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public Stream EndOpenRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndReleaseLease(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndRenewLease(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndSetMetadata(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndSetProperties(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndUploadFromByteArray(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndUploadFromFile(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public void EndUploadFromStream(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public bool Exists(BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void FetchAttributes(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task FetchAttributesAsync()
        {
            throw new NotImplementedException();
        }

        public Task FetchAttributesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public AccountProperties GetAccountProperties(BlobRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<AccountProperties> GetAccountPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AccountProperties> GetAccountPropertiesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AccountProperties> GetAccountPropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<AccountProperties> GetAccountPropertiesAsync(BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy)
        {
            throw new NotImplementedException();
        }

        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, string groupPolicyIdentifier)
        {
            throw new NotImplementedException();
        }

        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers)
        {
            throw new NotImplementedException();
        }

        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier)
        {
            throw new NotImplementedException();
        }

        public string GetSharedAccessSignature(SharedAccessBlobPolicy policy, SharedAccessBlobHeaders headers, string groupPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
        {
            throw new NotImplementedException();
        }

        public Stream OpenRead(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void ReleaseLease(AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(AccessCondition accessCondition)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void RenewLease(AccessCondition accessCondition, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task RenewLeaseAsync(AccessCondition accessCondition)
        {
            throw new NotImplementedException();
        }

        public Task RenewLeaseAsync(AccessCondition accessCondition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetMetadata(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task SetMetadataAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetMetadataAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetProperties(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task SetPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetPropertiesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task SetPropertiesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void UploadFromByteArray(byte[] buffer, int index, int count, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromByteArrayAsync(byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void UploadFromFile(string path, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromFileAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromFileAsync(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromFileAsync(string path, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void UploadFromStream(Stream source, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public void UploadFromStream(Stream source, long length, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, long length)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, long length, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Task UploadFromStreamAsync(Stream source, long length, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
