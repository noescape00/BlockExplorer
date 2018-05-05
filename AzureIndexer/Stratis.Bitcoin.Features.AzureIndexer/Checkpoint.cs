﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Stratis.Bitcoin.Features.AzureIndexer
{
    public class Checkpoint
    {
        private readonly string _CheckpointName;

        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;

        public string CheckpointName
        {
            get
            {
                return _CheckpointName;
            }
        }

        CloudBlockBlob _Blob;
        public Checkpoint(string checkpointName, Network network, Stream data, CloudBlockBlob blob, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(GetType().FullName);

            if (checkpointName == null)
                throw new ArgumentNullException("checkpointName");
            _Blob = blob;
            _CheckpointName = checkpointName;
            _BlockLocator = new BlockLocator();
            if (data != null)
            {
                try
                {
                    _BlockLocator.ReadWrite(data, false);
                    return;
                }
                catch
                {
                }
            }
            var list = new List<uint256>();
            list.Add(network.GetGenesis().Header.GetHash());
            _BlockLocator = new BlockLocator();
            _BlockLocator.Blocks.AddRange(list);
        }

        public uint256 Genesis
        {
            get
            {
                return BlockLocator.Blocks[BlockLocator.Blocks.Count - 1];
            }
        }

        BlockLocator _BlockLocator;
        public BlockLocator BlockLocator
        {
            get
            {
                return _BlockLocator;
            }
        }

        public bool SaveProgress(ChainedBlock tip)
        {
            this.logger.LogTrace("()");

            var result = SaveProgress(tip.GetLocator());

            this.logger.LogTrace("(-):{0}", result);

            return result;
        }
        public bool SaveProgress(BlockLocator locator)
        {
            this.logger.LogTrace("()");

            _BlockLocator = locator;
            try
            {
                var result = SaveProgressAsync().Result;

                this.logger.LogTrace("(-):{0}", result);

                return result;
            }
            catch (AggregateException aex)
            {
                ExceptionDispatchInfo.Capture(aex.InnerException).Throw();

                this.logger.LogTrace("(-):false");
                return false;
            }
        }

        public async Task DeleteAsync()
        {
            try
            {
                await _Blob.DeleteAsync().ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation != null && ex.RequestInformation.HttpStatusCode == 404)
                    return;
                throw;
            }
        }

        private async Task<bool> SaveProgressAsync()
        {
            this.logger.LogTrace("()");

            var bytes = BlockLocator.ToBytes();

            this.logger.LogTrace("Block locator converted to bytes");

            try
            {

                await _Blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length, new AccessCondition()
                {
                    IfMatchETag = _Blob.Properties.ETag
                }, null, null).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation != null && ex.RequestInformation.HttpStatusCode == 412)
                {
                    this.logger.LogTrace("(-): false");
                    return false;
                }

                throw;
            }

            this.logger.LogTrace("(-): true");
            return true;
        }

        public static async Task<Checkpoint> LoadBlobAsync(CloudBlockBlob blob, Network network, ILoggerFactory loggerFactory)
        {
            var checkpointName = string.Join("/", blob.Name.Split('/').Skip(1).ToArray());
            MemoryStream ms = new MemoryStream();
            try
            {
                await blob.DownloadToStreamAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation == null || ex.RequestInformation.HttpStatusCode != 404)
                    throw;
            }
            var checkpoint = new Checkpoint(checkpointName, network, ms, blob, loggerFactory);
            return checkpoint;
        }

        public override string ToString()
        {
            return CheckpointName;
        }
    }

}
