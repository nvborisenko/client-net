﻿using ReportPortal.Client.Abstractions.Filtering;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Client.IntegrationTests.LaunchItem
{
    public class LaunchItemFixture : BaseFixture
    {
        [Fact]
        public async Task GetInvalidLaunch()
        {
            await Assert.ThrowsAsync<ReportPortalException>(async () => await Service.Launch.GetAsync("invalid_id"));
        }

        [Fact]
        public async Task GetLaunches()
        {
            var container = await Service.Launch.GetAsync();
            var launches = container.Items.ToList();
            Assert.True(launches.Count() > 0);
        }

        [Fact]
        public async Task GetDebugLaunches()
        {
            var launches = await Service.Launch.GetDebugAsync();
            launches.Items.ToList().ForEach((l) => Assert.Equal(LaunchMode.Debug, l.Mode));
        }

        [Fact]
        public async Task GetTheFirst10Launches()
        {
            var launches = await Service.Launch.GetAsync(new FilterOption
            {
                Paging = new Paging(1, 10)
            });
            Assert.Equal(10, launches.Items.Count());
        }

        [Fact]
        public async Task GetLaunchesFilteredByName()
        {
            var launches = await Service.Launch.GetAsync(new FilterOption
            {
                Paging = new Paging(1, 10),
                Filters = new List<Filter> { new Filter(FilterOperation.Contains, "name", "test") }
            });
            Assert.True(launches.Items.Count() > 0);
            foreach (var launch in launches.Items)
            {
                Assert.Contains("test", launch.Name.ToLower());
            }
        }

        [Fact]
        public async Task GetLaunchesSortedByAscendingDate()
        {
            var launches = await Service.Launch.GetAsync(new FilterOption
            {
                Paging = new Paging(1, 10),
                Sorting = new Sorting(new List<string> { "startTime" }, SortDirection.Ascending)
            });

            Assert.True(launches.Items.Count() > 0);

            Assert.Equal(launches.Items.Select(l => l.StartTime).OrderBy(st => st), launches.Items.Select(l => l.StartTime));
        }

        [Fact]
        public async Task GetLaunchesSortedByDescendingDate()
        {
            var launches = await Service.Launch.GetAsync(new FilterOption
            {
                Paging = new Paging(1, 10),
                Sorting = new Sorting(new List<string> { "startTime" }, SortDirection.Descending)
            });

            Assert.True(launches.Items.Count() > 0);

            Assert.Equal(launches.Items.Select(l => l.StartTime).OrderByDescending(st => st), launches.Items.Select(l => l.StartTime));
        }

        [Fact]
        public async Task StartFinishDeleteLaunch()
        {
            var startLaunchRequest = new StartLaunchRequest
            {
                Name = "StartFinishDeleteLaunch",
                StartTime = DateTime.UtcNow
            };

            var launch = await Service.Launch.StartAsync(startLaunchRequest);
            Assert.NotNull(launch.Uuid);
            Assert.NotEqual(0, launch.Number);

            var finishLaunchRequest = new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow.AddHours(1)
            };

            var message = await Service.Launch.FinishAsync(launch.Uuid, finishLaunchRequest);
            Assert.Equal(launch.Uuid, message.Uuid);

            var gotLaunch = await Service.Launch.GetAsync(launch.Uuid);
            Assert.Equal("StartFinishDeleteLaunch", gotLaunch.Name);
            Assert.Equal(startLaunchRequest.StartTime, gotLaunch.StartTime);
            Assert.Equal(finishLaunchRequest.EndTime, gotLaunch.EndTime);

            var delMessage = await Service.Launch.DeleteAsync(gotLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task StartUpdateFinishDeleteLaunch()
        {
            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartFinishDeleteLaunch",
                StartTime = DateTime.UtcNow
            });

            var tempLaunch = await Service.Launch.GetAsync(launch.Uuid);

            var updateMessage = await Service.Launch.UpdateAsync(tempLaunch.Id, new UpdateLaunchRequest()
            {
                Description = "New description",
                Mode = LaunchMode.Debug,
                Tags = null
            });

            Assert.NotNull(launch.Uuid);
            Assert.Contains("successfully updated", updateMessage.Info);
            var message = await Service.Launch.FinishAsync(launch.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Equal(launch.Uuid, message.Uuid);

            var gotLaunch = await Service.Launch.GetAsync(launch.Uuid);
            Assert.Equal("StartFinishDeleteLaunch", gotLaunch.Name);

            var delMessage = await Service.Launch.DeleteAsync(gotLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task StartFinishDeleteFullLaunch()
        {
            var now = DateTime.UtcNow;
            var attributes = new List<ItemAttribute> { new ItemAttribute { Key = "a1", Value = "v1" }, new ItemAttribute { Key = "a2", Value = "v2" }, new ItemAttribute { Key = "", Value = "v3" }, new ItemAttribute { Key = null, Value = "v4" } };
            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartFinishDeleteFullLaunch",
                Description = "Desc",
                StartTime = now,
                Attributes = attributes
            });
            Assert.NotNull(launch.Uuid);
            var getLaunch = await Service.Launch.GetAsync(launch.Uuid);
            Assert.Equal("StartFinishDeleteFullLaunch", getLaunch.Name);
            Assert.Equal("Desc", getLaunch.Description);
            Assert.Equal(now.ToString(), getLaunch.StartTime.ToString());

            Assert.Equal(attributes.OrderBy(a => a.Key).Select(a => new { a.Key, a.Value }).ToList(), getLaunch.Attributes.OrderBy(a => a.Key).Select(a => new { a.Key, a.Value }).ToList());
            var message = await Service.Launch.FinishAsync(launch.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Equal(launch.Uuid, message.Uuid);
            var delMessage = await Service.Launch.DeleteAsync(getLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task StartFinishDeleteMergedLaunch()
        {
            var launch1 = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartFinishDeleteLaunch",
                StartTime = DateTime.UtcNow
            });
            Assert.NotNull(launch1.Uuid);
            var message = await Service.Launch.FinishAsync(launch1.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Equal(launch1.Uuid, message.Uuid);

            var launch2 = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartFinishDeleteLaunch2",
                StartTime = DateTime.UtcNow
            });
            Assert.NotNull(launch2.Uuid);
            message = await Service.Launch.FinishAsync(launch2.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Equal(launch2.Uuid, message.Uuid);

            var getLaunch1 = await Service.Launch.GetAsync(launch1.Uuid);
            var getLaunch2 = await Service.Launch.GetAsync(launch2.Uuid);
            var mergeRequest = new MergeLaunchesRequest
            {
                Name = "MergedLaunch",
                Launches = new List<long> { getLaunch1.Id, getLaunch2.Id },
                MergeType = "BASIC",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };

            var mergedLaunch = await Service.Launch.MergeAsync(mergeRequest);
            Assert.Equal(mergeRequest.StartTime, mergedLaunch.StartTime);
            Assert.Equal(mergeRequest.EndTime, mergedLaunch.EndTime);

            var delMessage = await Service.Launch.DeleteAsync(mergedLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task StartFinishAnalyzeDeleteLaunch()
        {
            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartFinishDeleteLaunch",
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default
            });
            Assert.NotNull(launch.Uuid);
            var message = await Service.Launch.FinishAsync(launch.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Equal(launch.Uuid, message.Uuid);

            var gotLaunch = await Service.Launch.GetAsync(launch.Uuid);
            Assert.Equal("StartFinishDeleteLaunch", gotLaunch.Name);

            var analyzeMessage = await Service.Launch.AnalyzeAsync(new AnalyzeLaunchRequest
            {
                LaunchId = gotLaunch.Id,
                AnalyzerMode = AnalyzerMode.LaunchName,
                AnalyzerTypeName = "autoAnalyzer",
                AnalyzerItemsMode = new List<AnalyzerItemsMode> { AnalyzerItemsMode.ToInvestigate }
            });
            Assert.Contains("started", analyzeMessage.Info);

            var delMessage = await Service.Launch.DeleteAsync(gotLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task TrimLaunchName()
        {
            var namePrefix = "TrimLaunch";
            var launchName = namePrefix + new string('_', 256 - namePrefix.Length + 1);

            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = launchName,
                StartTime = DateTime.UtcNow
            });
            Assert.NotNull(launch.Uuid);
            var message = await Service.Launch.FinishAsync(launch.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
            Assert.Contains(launch.Uuid, message.Uuid);

            var gotLaunch = await Service.Launch.GetAsync(launch.Uuid);
            Assert.Equal(launchName.Substring(0, 256), gotLaunch.Name);

            var delMessage = await Service.Launch.DeleteAsync(gotLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task StartForceFinishIncompleteLaunch()
        {
            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartForceFinishIncompleteLaunch",
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default
            });

            var test = await Service.TestItem.StartAsync(new StartTestItemRequest
            {
                LaunchUuid = launch.Uuid,
                Name = "Test1",
                StartTime = DateTime.UtcNow,
                Type = TestItemType.Test
            });
            Assert.NotNull(test.Uuid);

            var tempLaunch = await Service.Launch.GetAsync(launch.Uuid);

            await Service.Launch.StopAsync(tempLaunch.Id, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });

            var getLaunch = await Service.Launch.GetAsync(launch.Uuid);

            var delMessage = await Service.Launch.DeleteAsync(getLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task GetInProgressLaunch()
        {
            var launch = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = "StartForceFinishIncompleteLaunch",
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default
            });

            var getLaunch = await Service.Launch.GetAsync(launch.Uuid);

            Assert.NotEqual(default(DateTime), getLaunch.StartTime);
            Assert.Null(getLaunch.EndTime);

            await Service.Launch.FinishAsync(launch.Uuid, new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });

            var delMessage = await Service.Launch.DeleteAsync(getLaunch.Id);
            Assert.Contains("successfully", delMessage.Info);
        }

        [Fact]
        public async Task RerunLaunch()
        {
            var name = Guid.NewGuid().ToString();

            var launch1Response = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = name,
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default
            });

            var launch2Response = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = name,
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default,
                IsRerun = true
            });

            Assert.Equal(launch1Response.Uuid, launch2Response.Uuid);

            await Service.Launch.FinishAsync(launch1Response.Uuid, new FinishLaunchRequest { EndTime = DateTime.UtcNow });

            // api doesn't allow to finish launch twice?! So when using rerun, we can start launch, but it seems we should not finish launch
            await Assert.ThrowsAnyAsync<ReportPortalException>(() => Service.Launch.FinishAsync(launch2Response.Uuid, new FinishLaunchRequest { EndTime = DateTime.UtcNow }));

            var gotLaunch = await Service.Launch.GetAsync(launch1Response.Uuid);
            await Service.Launch.DeleteAsync(gotLaunch.Id);
        }

        [Fact]
        public async Task RerunToSpecificLaunch()
        {
            var name = Guid.NewGuid().ToString();

            var launch1Response = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = name,
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default
            });

            var launch2Response = await Service.Launch.StartAsync(new StartLaunchRequest
            {
                Name = name,
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default,
                // if we want to rerun specific launch, we have to set IsRerun=true also. Otherwise api creates new launch
                IsRerun = true,
                RerunOfLaunchUuid = launch1Response.Uuid
            });

            Assert.Equal(launch1Response.Uuid, launch2Response.Uuid);

            await Service.Launch.FinishAsync(launch1Response.Uuid, new FinishLaunchRequest { EndTime = DateTime.UtcNow });

            // api doesn't allow to finish launch twice?! So when using rerun, we can start launch, but it seems we should not finish launch
            await Assert.ThrowsAnyAsync<ReportPortalException>(() => Service.Launch.FinishAsync(launch2Response.Uuid, new FinishLaunchRequest { EndTime = DateTime.UtcNow }));

            var gotLaunch = await Service.Launch.GetAsync(launch1Response.Uuid);
            await Service.Launch.DeleteAsync(gotLaunch.Id);
        }

        [Fact]
        public async Task RerunNonExistingLaunch()
        {
            var request = new StartLaunchRequest
            {
                Name = "Some unique " + Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                Mode = LaunchMode.Default,
                IsRerun = true
            };

            await Assert.ThrowsAnyAsync<ReportPortalException>(() => Service.Launch.StartAsync(request));
        }
    }
}
