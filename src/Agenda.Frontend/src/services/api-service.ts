import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Appointment } from '../models/appointment';
import { map, Observable } from 'rxjs';
import { NewAppointmentPayload } from '../models/new-appointment-payload';
import { Browsable } from '../models/browsable';
import { PageLink, PageLinks, PageOf } from '../models/page-of';
import { SearchAppointmentsParams } from '../models/search-appointments-params';

@Injectable({
  providedIn: 'root',
  deps: [HttpClient]
})

/**
 * Service for interacting with the API.
 */
export class ApiService {
  constructor(public http: HttpClient) {
    this.http = http;
  }

  /** Gets paginated appointments from the API */
  public getAppointments(params?: SearchAppointmentsParams): Observable<PageOf<Browsable<Appointment>>> {
    let httpParams: HttpParams = new HttpParams();

    if (params) {
      if (params.page !== undefined) {
        httpParams = httpParams.set('page', params.page.toString());
      }
      if (params.pageSize !== undefined) {
        httpParams = httpParams.set('pageSize', params.pageSize.toString());
      }
      if (params.subject !== undefined && params.subject.trim()) {
        httpParams = httpParams.set('subject', params.subject);
      }
      if (params.location !== undefined && params.location.trim()) {
        httpParams = httpParams.set('location', params.location);
      }
      if (params.from !== undefined) {
        httpParams = httpParams.set('from', params.from);
      }
      if (params.to !== undefined) {
        httpParams = httpParams.set('to', params.to);
      }
      if (params.sort !== undefined) {
        httpParams = httpParams.set('sort', params.sort);
      }
    }

    return this.http
      .get<PageOf<Browsable<Appointment>>>('/api/appointments', {
        params: httpParams,
        observe: 'response'
      })
      .pipe(
        map((response) => this.toPaginatedAppointmentsResponse(response, params?.pageSize))
      );
  }

  /** Creates a new appointment. */
  public scheduleAppointment(payload: NewAppointmentPayload): Observable<Browsable<Appointment>> {
    return this.http.post<Browsable<Appointment>>('/api/appointments', payload);
  }

  private toPaginatedAppointmentsResponse(
    response: HttpResponse<PageOf<Browsable<Appointment>>>,
    requestedPageSize?: number
  ): PageOf<Browsable<Appointment>> {
    const body = this.normalizePaginatedBody(response.body);

    const linksFromHeaders = this.extractPageLinksFromHeaders(response.headers);
    const mergedLinks: PageLinks = {
      ...(body.links ?? {}),
      ...linksFromHeaders
    };

    const totalItemsFromHeaders = this.parsePositiveInteger(response.headers.get('total'));
    const countFromHeaders = this.parsePositiveInteger(response.headers.get('count'));
    const currentPage = this.normalizePageNumber(body.page);
    const pageSize = this.resolvePageSize(body, requestedPageSize, countFromHeaders);
    const totalPagesFromLinks = this.extractLastPageFromLinks(mergedLinks);
    const totalPagesFromHeaders = totalItemsFromHeaders !== null
      ? this.normalizePageNumber(Math.ceil(totalItemsFromHeaders / pageSize))
      : null;
    const totalPages = totalPagesFromLinks
      ?? totalPagesFromHeaders
      ?? this.normalizePageNumber(body.total)
      ?? 1;

    return {
      ...body,
      page: Math.min(currentPage, totalPages),
      total: totalPages,
      count: countFromHeaders ?? body.count ?? body.items.length,
      links: mergedLinks
    };
  }

  private normalizePaginatedBody(responseBody: PageOf<Browsable<Appointment>> | null): PageOf<Browsable<Appointment>> {
    const rawBody = this.toRecord(responseBody);
    if (!rawBody) {
      return {
        page: 1,
        total: 1,
        count: 0,
        items: [],
        links: {}
      };
    }

    const rawPageSize = this.readNumber(rawBody, 'pageSize', 'PageSize');
    const rawTotalCount = this.readNumber(rawBody, 'totalCount', 'TotalCount');

    return {
      page: this.readNumber(rawBody, 'page', 'Page') ?? 1,
      total: this.readNumber(rawBody, 'total', 'Total') ?? 1,
      count: this.readNumber(rawBody, 'count', 'Count') ?? 0,
      pageSize: rawPageSize ?? undefined,
      totalCount: rawTotalCount ?? undefined,
      items: this.normalizeBrowsables(rawBody['items'] ?? rawBody['Items']),
      links: this.normalizePageLinks(rawBody['links'] ?? rawBody['Links'])
    };
  }

  private normalizeBrowsables(rawItems: unknown): Browsable<Appointment>[] {
    if (!Array.isArray(rawItems)) {
      return [];
    }

    return rawItems
      .map((rawItem) => this.normalizeBrowsable(rawItem))
      .filter((item): item is Browsable<Appointment> => item !== null);
  }

  private normalizeBrowsable(rawItem: unknown): Browsable<Appointment> | null {
    const item = this.toRecord(rawItem);
    if (!item) {
      return null;
    }

    const resource = this.normalizeAppointment(item['resource'] ?? item['Resource']);
    if (!resource) {
      return null;
    }

    const rawLinks = item['links'] ?? item['Links'];

    return {
      resource,
      links: Array.isArray(rawLinks)
        ? rawLinks as Array<{ href: string; method?: string; relations?: string[] }>
        : []
    };
  }

  private normalizeAppointment(rawResource: unknown): Appointment | null {
    const resource = this.toRecord(rawResource);
    if (!resource) {
      return null;
    }

    const id = this.readString(resource, 'id', 'Id');
    const subject = this.readString(resource, 'subject', 'Subject');
    const location = this.readString(resource, 'location', 'Location');
    const startDate = this.readString(resource, 'startDate', 'StartDate');
    const endDate = this.readString(resource, 'endDate', 'EndDate');

    if (!id || !subject || !location || !startDate || !endDate) {
      return null;
    }

    const rawAttendees = resource['attendees'] ?? resource['Attendees'];

    return {
      id,
      subject,
      location,
      startDate,
      endDate,
      attendees: Array.isArray(rawAttendees)
        ? rawAttendees as Appointment['attendees']
        : []
    };
  }

  private normalizePageLinks(rawLinks: unknown): PageLinks {
    const links = this.toRecord(rawLinks);
    if (!links) {
      return {};
    }

    return {
      first: this.normalizePageLink(links['first'] ?? links['First']),
      last: this.normalizePageLink(links['last'] ?? links['Last']),
      previous: this.normalizePageLink(links['previous'] ?? links['Previous']),
      next: this.normalizePageLink(links['next'] ?? links['Next'])
    };
  }

  private normalizePageLink(rawLink: unknown): PageLink | undefined {
    const link = this.toRecord(rawLink);
    if (!link) {
      return undefined;
    }

    const href = this.readString(link, 'href', 'Href');
    if (!href) {
      return undefined;
    }

    const rawRelations = link['relations'] ?? link['Relations'];
    const relations = Array.isArray(rawRelations)
      ? rawRelations as string[]
      : [];

    return {
      href,
      relations
    };
  }

  private toRecord(value: unknown): Record<string, unknown> | null {
    if (typeof value !== 'object' || value === null) {
      return null;
    }

    return value as Record<string, unknown>;
  }

  private readNumber(source: Record<string, unknown>, camelCaseKey: string, pascalCaseKey: string): number | null {
    const rawValue = source[camelCaseKey] ?? source[pascalCaseKey];
    if (typeof rawValue !== 'number' || !Number.isFinite(rawValue)) {
      return null;
    }

    return rawValue;
  }

  private readString(source: Record<string, unknown>, camelCaseKey: string, pascalCaseKey: string): string | null {
    const rawValue = source[camelCaseKey] ?? source[pascalCaseKey];
    if (typeof rawValue !== 'string') {
      return null;
    }

    return rawValue;
  }

  private extractPageLinksFromHeaders(headers: HttpHeaders): PageLinks {
    const parsedLinks: PageLinks = {};
    const values = headers.getAll('Link') ?? [];
    const linkPattern = /<([^>]+)>\s*;\s*rel="([^"]+)"/gi;

    values.forEach((headerValue) => {
      const matches = headerValue.matchAll(linkPattern);
      Array.from(matches).forEach((match) => {
        const href = match[1]?.trim();
        const relationValue = match[2]?.trim();

        if (!href || !relationValue) {
          return;
        }

        relationValue
          .split(/\s+/)
          .map((relation) => relation.trim().toLowerCase())
          .filter(Boolean)
          .forEach((relation) => {
            const normalizedRelation = relation === 'prev' ? 'previous' : relation;
            const link: PageLink = { href, relations: [normalizedRelation] };

            switch (normalizedRelation) {
              case 'first':
                parsedLinks.first = parsedLinks.first ?? link;
                break;
              case 'last':
                parsedLinks.last = parsedLinks.last ?? link;
                break;
              case 'previous':
                parsedLinks.previous = parsedLinks.previous ?? link;
                break;
              case 'next':
                parsedLinks.next = parsedLinks.next ?? link;
                break;
              default:
                break;
            }
          });
      });
    });

    return parsedLinks;
  }

  private extractLastPageFromLinks(links: PageLinks): number | null {
    return this.extractPageNumber(links.last?.href);
  }

  private extractPageNumber(href?: string): number | null {
    if (!href) {
      return null;
    }

    try {
      const url = new URL(href, window.location.origin);
      const page = Number.parseInt(url.searchParams.get('page') ?? '', 10);
      return Number.isNaN(page) ? null : this.normalizePageNumber(page);
    } catch {
      return null;
    }
  }

  private parsePositiveInteger(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number.parseInt(value, 10);
    if (Number.isNaN(parsed) || parsed < 0) {
      return null;
    }

    return parsed;
  }

  private resolvePageSize(
    body: PageOf<Browsable<Appointment>>,
    requestedPageSize?: number,
    countFromHeaders?: number | null
  ): number {
    const candidates = [body.pageSize, requestedPageSize, body.count, countFromHeaders, body.items.length, 1];
    const firstPositiveCandidate = candidates.find((candidate) =>
      typeof candidate === 'number'
      && Number.isFinite(candidate)
      && candidate > 0
    );

    return firstPositiveCandidate ?? 1;
  }

  private normalizePageNumber(page: number | null | undefined): number {
    if (typeof page !== 'number' || !Number.isFinite(page) || page <= 0) {
      return 1;
    }

    return Math.floor(page);
  }
}
